/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Net;
using System.Text;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;
using Microsoft.Extensions.Logging;
using Prometheus;
using PrometheusMetrics = Prometheus.Metrics;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter;

/// <summary>
/// HTTP endpoint: every request reads the cluster, then answers with the metrics of that read.
/// If no node answers or the login fails the answer is 503, so Prometheus sets up = 0 for the target.
/// </summary>
internal sealed class PrometheusServer
{
    private const string ContentType = "text/plain; version=0.0.4; charset=utf-8";

    private readonly Func<Task<PveClient>> _clientFactory;
    private readonly CollectorRegistry _registry = PrometheusMetrics.NewCustomRegistry();
    private readonly MetricsEngine _engine;
    private readonly ILogger<PrometheusServer> _logger;
    private readonly HttpListener _listener = new();

    // The engine keeps state between collections: concurrent scrapes read the cluster one at a time.
    private readonly SemaphoreSlim _collectLock = new(1, 1);
    private CancellationTokenSource? _cts;

    public PrometheusServer(Func<Task<PveClient>> clientFactory,
                            Api.Prometheus.Settings settings,
                            ILoggerFactory loggerFactory)
    {
        _clientFactory = clientFactory;
        _engine = new MetricsEngine(settings, _registry, loggerFactory.CreateLogger<MetricsEngine>());
        _logger = loggerFactory.CreateLogger<PrometheusServer>();
        _listener.Prefixes.Add($"http://{settings.Host}:{settings.Port}/{settings.Url}");
    }

    public void Start()
    {
        _listener.Start();
        _cts = new CancellationTokenSource();
        _ = AcceptLoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        if (_listener.IsListening) { _listener.Stop(); }
    }

    private async Task AcceptLoopAsync(CancellationToken cancel)
    {
        while (!cancel.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (cancel.IsCancellationRequested || !_listener.IsListening)
            {
                return;
            }

            _ = Task.Run(() => HandleAsync(context, cancel), cancel);
        }
    }

    private async Task HandleAsync(HttpListenerContext context, CancellationToken cancel)
    {
        var response = context.Response;
        try
        {
            await _collectLock.WaitAsync(cancel);
            try
            {
                PveClient client;
                try
                {
                    client = await _clientFactory();
                }
                catch (Exception ex)
                {
                    _logger.LogError("Cannot connect to Proxmox VE: {Message}", ex.Message);
                    await WriteAsync(response, HttpStatusCode.ServiceUnavailable, $"Cannot connect to Proxmox VE: {ex.Message}");
                    return;
                }

                await _engine.CollectAsync(client);

                using var body = new MemoryStream();
                await _registry.CollectAndExportAsTextAsync(body, cancel);
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = ContentType;
                response.ContentLength64 = body.Length;
                body.Position = 0;
                await body.CopyToAsync(response.OutputStream, cancel);
            }
            finally
            {
                _collectLock.Release();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Collection failed");
            try { await WriteAsync(response, HttpStatusCode.InternalServerError, $"Collection failed: {ex.Message}"); }
            catch { /* response already started */ }
        }
        finally
        {
            try { response.Close(); } catch { /* client gone */ }
        }
    }

    private static async Task WriteAsync(HttpListenerResponse response, HttpStatusCode status, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        response.StatusCode = (int)status;
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
    }
}
