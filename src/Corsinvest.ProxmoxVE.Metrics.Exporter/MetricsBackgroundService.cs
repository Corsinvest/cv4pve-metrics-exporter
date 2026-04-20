/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter;

internal sealed class MetricsBackgroundService(ILogger<MetricsBackgroundService> logger,
                                               ILoggerFactory loggerFactory,
                                               MetricsServiceOptions options,
                                               IHostApplicationLifetime appLifetime) : BackgroundService
{
    private PrometheusServer? _server;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var prom = options.Settings.Prometheus;

            if (!prom.Enabled)
            {
                logger.LogWarning("No exporter enabled in settings, exiting.");
                appLifetime.StopApplication();
                return Task.CompletedTask;
            }

            _server = new PrometheusServer(options.ClientFactory, prom, loggerFactory);

            Console.Out.WriteLine("Corsinvest for Proxmox VE");
            Console.Out.WriteLine($"Prometheus: http://{prom.Host}:{prom.Port}/{prom.Url}");
            Console.Out.WriteLine("Press Ctrl+C to stop.");

            _server.Start();
            logger.LogInformation("Prometheus exporter started");

            return Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error in metrics service");
            appLifetime.StopApplication();
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("MetricsBackgroundService stopping...");
        _server?.Stop();
        await base.StopAsync(cancellationToken);
    }
}
