/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Parse command line arguments using ConsoleHelper
var app = ConsoleHelper.CreateApp("cv4pve-metrics-exporter", "Metrics Exporter for Proxmox VE");
var optServiceMode = app.AddOption<bool>("--service-mode", "Run as background service (runs until stopped, no Enter key)");

var cmd = app.AddCommand("prometheus", "Export for Prometheus");

var optHttpHost = cmd.AddOption<string>("--http-host", $"Http host");
optHttpHost.DefaultValueFactory = (_) => PrometheusExporter.DEFAULT_HOST;

var optHttpPort = cmd.AddOption<int>("--http-port", $"Http port");
optHttpPort.DefaultValueFactory = (_) => PrometheusExporter.DEFAULT_PORT;

var optHttpUrl = cmd.AddOption<string>("--http-url", $"Http url");
optHttpUrl.DefaultValueFactory = (_) => PrometheusExporter.DEFAULT_URL;

var optPrefix = cmd.AddOption<string>("--prefix", $"Prefix export");
optPrefix.DefaultValueFactory = (_) => PrometheusExporter.DEFAULT_PREFIX;

cmd.SetAction(async (action) =>
{
    // Create host builder with dependency injection
    var hostBuilder = Host.CreateDefaultBuilder()
        .ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();

            // Apply same filtering as ConsoleHelper.CreateLoggerFactory
            var logLevel = app.GetLogLevelFromDebug();
            logging.AddFilter("Microsoft", LogLevel.Warning);
            logging.AddFilter("System", LogLevel.Warning);
            logging.AddFilter("Corsinvest.ProxmoxVE.Api.PveClientBase", logLevel);
            logging.SetMinimumLevel(logLevel);
        })
        .ConfigureServices((context, services) =>
        {
            // Register metrics exporter configuration options
            services.AddSingleton(new MetricsServiceOptions
            {
                Host = action.GetValue(app.GetHostOption())!,
                Username = action.GetValue(app.GetUsernameOption())!,
                Password = app.GetPasswordFromOption(),
                ApiToken = action.GetValue(app.GetApiTokenOption()),
                ValidateCertificate = action.GetValue(app.GetValidateCertificateOption()),
                HttpHost = action.GetValue(optHttpHost)!,
                HttpPort = action.GetValue(optHttpPort),
                HttpUrl = action.GetValue(optHttpUrl)!,
                Prefix = action.GetValue(optPrefix)!,
                ServiceMode = action.GetValue(optServiceMode)
            });

            // Register the metrics background service
            services.AddHostedService<MetricsBackgroundService>();
        });

    var host = hostBuilder.Build();

    // Run in appropriate mode
    if (action.GetValue(optServiceMode) == false)
    {
        // Console mode - allow manual stop with Enter key
        await host.StartAsync();

        Console.WriteLine("Metrics exporter is running. Press Enter to stop...");
        Console.ReadLine();

        Console.WriteLine("Stopping metrics exporter...");
        await host.StopAsync(TimeSpan.FromSeconds(10));
    }
    else
    {
        // Service mode - run indefinitely until process is killed
        await host.RunAsync();
    }
});

var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());

return await app.ExecuteAppAsync(args, loggerFactory.CreateLogger<Program>());
