/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System;
using System.CommandLine;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = ConsoleHelper.CreateApp("cv4pve-metrics-exporter", "Metrics Exporter for Proxmox VE");

            var cmd = app.AddCommand("prometheus", "Export for Prometheus");
            var optHost = cmd.AddOption("--http-host", $"Http host (default: {PrometheusExporter.DEFAULT_HOST})");
            optHost.SetDefaultValue(PrometheusExporter.DEFAULT_HOST);

            var optPort = cmd.AddOption<int>("--http-port", $"Http port (default: {PrometheusExporter.DEFAULT_PORT})");
            optPort.SetDefaultValue(PrometheusExporter.DEFAULT_PORT);

            var optUrl = cmd.AddOption("--http-url", $"Http url (default: {PrometheusExporter.DEFAULT_URL})");
            optUrl.SetDefaultValue(PrometheusExporter.DEFAULT_URL);

            var optPrefix = cmd.AddOption("--prefix", $"Prefix export (default: {PrometheusExporter.DEFAULT_PREFIX})");
            optPrefix.SetDefaultValue(PrometheusExporter.DEFAULT_PREFIX);

            var optNodeDiskInfo = cmd.AddOption("--node-disk-info", "Export disk info (disk,wearout,smart)");

            cmd.SetHandler(() =>
            {
                var logFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());

                var exporter = new PrometheusExporter(app.GetHost().GetValue(),
                                                      app.GetUsername().GetValue(),
                                                      app.GetPasswordFromOption(),
                                                      app.GetApiToken().GetValue(),
                                                      logFactory,
                                                      optHost.GetValue(),
                                                      optPort.GetValue(),
                                                      optUrl.GetValue(),
                                                      optPrefix.GetValue(),
                                                      optNodeDiskInfo.HasValue());

                exporter.Start();

                Console.Out.WriteLine("Corsinvest for Proxmox VE");
                Console.Out.WriteLine($"Cluster: {app.GetHost().GetValue()} - User: {app.GetUsername().GetValue()}");
                Console.Out.WriteLine($"Exporter Prometheus: http://{optHost.GetValue()}:{optPort.GetValue()}/{optUrl.GetValue()} - Prefix: {optPrefix.GetValue()}");
                Console.Out.WriteLine($"Export Node Disk Info: {optNodeDiskInfo.HasValue()}");

                Console.ReadLine();

                try { exporter.Stop(); }
                catch { }

                Console.Out.WriteLine("End application");
            });

            app.ExecuteApp(args);
        }
    }
}
