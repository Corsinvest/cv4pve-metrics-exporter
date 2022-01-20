/*
 * This file is part of the cv4pve-metrics-exporter https://github.com/Corsinvest/cv4pve-metrics-exporter,
 *
 * This source file is available under two different licenses:
 * - GNU General Public License version 3 (GPLv3)
 * - Corsinvest Enterprise License (CEL)
 * Full copyright and license information is available in
 * LICENSE.md which is distributed with this source code.
 *
 * Copyright (C) 2016 Corsinvest Srl	GPLv3 and CEL
 */

using System;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api;
using McMaster.Extensions.CommandLineUtils;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = ShellHelper.CreateConsoleApp("cv4pve-metrics-exporter", "Metrics Exporter for Proxmox VE");

            app.Command("prometheus", cmd =>
            {
                cmd.Description = "Export for Prometheus";
                cmd.AddFullNameLogo();

                var optHost = cmd.Option("--http-host", $"Http host (default: {PrometheusExporter.DEFAULT_HOST})", CommandOptionType.SingleValue);
                var optPort = cmd.Option<int>("--http-port", $"Http port (default: {PrometheusExporter.DEFAULT_PORT})", CommandOptionType.SingleValue);
                var optUrl = cmd.Option("--http-url", $"Http url (default: {PrometheusExporter.DEFAULT_URL})", CommandOptionType.SingleValue);
                var optPrefix = cmd.Option("--prefix", $"Prefix export (default: {PrometheusExporter.DEFAULT_PREFIX})", CommandOptionType.SingleValue);
                var optNodeDiskInfo = cmd.Option("--node-disk-info", "Export disk info (disk,wearout,smart)", CommandOptionType.NoValue);

                cmd.OnExecute(() =>
                {
                    var hostsAndPortHA = app.GetHost().Value();
                    var host = optHost.HasValue() ? optHost.Value() : PrometheusExporter.DEFAULT_HOST;
                    var username = app.GetUsername().Value();
                    var password = app.GetPasswordFromOption();
                    var port = optPort.HasValue() ? optPort.ParsedValue : PrometheusExporter.DEFAULT_PORT;
                    var url = optUrl.HasValue() ? optUrl.Value() : PrometheusExporter.DEFAULT_URL;
                    var prefix = optPrefix.HasValue() ? optPrefix.Value() : PrometheusExporter.DEFAULT_PREFIX;

                    var client = ClientHelper.GetClientFromHA(hostsAndPortHA, null);
                    if (client.Login(username, password))
                    {
                        var exporter = new PrometheusExporter(hostsAndPortHA,
                                                              username,
                                                              password,
                                                              host,
                                                              port,
                                                              url,
                                                              prefix,
                                                              optNodeDiskInfo.HasValue());

                        exporter.Start();

                        app.Out.WriteLine("Corsinvest for Proxmox VE");
                        app.Out.WriteLine($"Cluster: {app.GetHost().Value()} - User: {app.GetUsername().Value()}");
                        app.Out.WriteLine($"Exporter Prometheus: http://{host}:{port}/{url} - Prefix: {prefix}");
                        app.Out.WriteLine($"Export Node Disk Info: {optNodeDiskInfo.HasValue()}");

                        Console.ReadLine();

                        try { exporter.Stop(); }
                        catch { }

                        app.Out.WriteLine("End application");
                    }
                    else
                    {
                        throw new ApplicationException("Error authentication!");
                    }
                });
            });

            app.ExecuteConsoleApp(args);
        }
    }
}
