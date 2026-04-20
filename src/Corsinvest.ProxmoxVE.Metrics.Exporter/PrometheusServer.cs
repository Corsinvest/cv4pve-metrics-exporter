/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;
using Microsoft.Extensions.Logging;
using Prometheus;
using PrometheusMetrics = Prometheus.Metrics;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter;

internal sealed class PrometheusServer
{
    private readonly MetricServer _server;

    public PrometheusServer(Func<Task<PveClient>> clientFactory,
                            Api.Prometheus.Settings settings,
                            ILoggerFactory loggerFactory)
    {
        var registry = PrometheusMetrics.NewCustomRegistry();
        var engine = new MetricsEngine(settings, registry, loggerFactory.CreateLogger<MetricsEngine>());

        registry.AddBeforeCollectCallback(async () =>
        {
            var client = await clientFactory();
            await engine.CollectAsync(client);
        });

        _server = new MetricServer(hostname: settings.Host, port: settings.Port, url: settings.Url, registry: registry);
    }

    public void Start() => _server.Start();

    public void Stop() => _server.Stop();
}
