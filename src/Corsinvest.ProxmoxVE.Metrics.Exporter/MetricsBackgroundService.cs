/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Metrics.Exporter.Api;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal sealed class MetricsBackgroundService(
    ILogger<MetricsBackgroundService> logger,
    ILoggerFactory loggerFactory,
    MetricsServiceOptions options,
    IHostApplicationLifetime appLifetime) : BackgroundService
{
    private PrometheusExporter? _exporter;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Create Prometheus exporter
            _exporter = new PrometheusExporter(
                options.Host,
                options.ValidateCertificate,
                options.Username,
                options.Password!,
                options.ApiToken!,
                loggerFactory,
                options.HttpHost,
                options.HttpPort,
                options.HttpUrl,
                options.Prefix);

            // Display startup information
            Console.Out.WriteLine("Corsinvest for Proxmox VE");
            Console.Out.WriteLine($"Cluster: {options.Host} - User: {options.Username}");
            Console.Out.WriteLine($"Exporter Prometheus: http://{options.HttpHost}:{options.HttpPort}/{options.HttpUrl} - Prefix: {options.Prefix}");

            logger.LogInformation("Starting Prometheus exporter in {Mode} mode...",
                options.ServiceMode ? "service" : "console");

            // Start the exporter
            _exporter.Start();
            logger.LogInformation("Prometheus exporter started successfully");

            // Register graceful shutdown
            stoppingToken.Register(() =>
            {
                logger.LogInformation("Shutdown requested, stopping Prometheus exporter...");
                try
                {
                    _exporter?.Stop();
                    logger.LogInformation("Prometheus exporter stopped successfully");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while stopping exporter");
                }
            });

            // Keep service running until cancellation
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                logger.LogDebug("Metrics service task cancelled");
            }
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
        _exporter?.Stop();
        await base.StopAsync(cancellationToken);
    }
}
