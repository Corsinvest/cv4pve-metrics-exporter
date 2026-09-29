/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter;

/// <summary>
/// Starts the HTTP endpoint in <see cref="StartAsync"/>, before the host reports it has started: with
/// systemd <c>Type=notify</c>, <c>systemctl start</c> returns only when the endpoint is listening.
/// </summary>
internal sealed class MetricsBackgroundService(ILogger<MetricsBackgroundService> logger,
                                               ILoggerFactory loggerFactory,
                                               MetricsServiceOptions options,
                                               IHostApplicationLifetime appLifetime) : IHostedService
{
    private PrometheusServer? _server;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var prom = options.Settings.Prometheus;

        if (!prom.Enabled)
        {
            logger.LogWarning("No exporter enabled in settings, exiting.");
            appLifetime.StopApplication();
            return Task.CompletedTask;
        }

        try
        {
            _server = new PrometheusServer(options.ClientFactory, prom, loggerFactory);
            _server.Start();
        }
        catch (Exception ex)
        {
            // E.g. the port is taken or the address cannot be listened on: exit with an error, so a service
            // manager set to restart on failure tries again.
            logger.LogCritical(ex, "Cannot start the HTTP endpoint http://{Host}:{Port}/{Url}", prom.Host, prom.Port, prom.Url);
            _server = null;
            options.ExitCode = 1;
            appLifetime.StopApplication();
            return Task.CompletedTask;
        }

        Console.Out.WriteLine("Corsinvest for Proxmox VE");
        Console.Out.WriteLine($"Prometheus: http://{prom.Host}:{prom.Port}/{prom.Url}");
        Console.Out.WriteLine("Press Ctrl+C to stop.");
        logger.LogInformation("Prometheus exporter started");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Prometheus exporter stopping");
        _server?.Stop();
        return Task.CompletedTask;
    }
}
