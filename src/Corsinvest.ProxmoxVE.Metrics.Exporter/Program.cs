/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System;
using System.CommandLine;
using Corsinvest.ProxmoxVE.Api.Shell.Helpers;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api;
using Microsoft.Extensions.Logging;

var app = ConsoleHelper.CreateApp("cv4pve-metrics-exporter", "Metrics Exporter for Proxmox VE");
var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());

var cmd = app.AddCommand("prometheus", "Export for Prometheus");
var optHost = cmd.AddOption<string>("--http-host", $"Http host (default: {PrometheusExporter.DEFAULT_HOST})");
optHost.SetDefaultValue(PrometheusExporter.DEFAULT_HOST);

var optPort = cmd.AddOption<int>("--http-port", $"Http port (default: {PrometheusExporter.DEFAULT_PORT})");
optPort.SetDefaultValue(PrometheusExporter.DEFAULT_PORT);

var optUrl = cmd.AddOption<string>("--http-url", $"Http url (default: {PrometheusExporter.DEFAULT_URL})");
optUrl.SetDefaultValue(PrometheusExporter.DEFAULT_URL);

var optPrefix = cmd.AddOption<string>("--prefix", $"Prefix export (default: {PrometheusExporter.DEFAULT_PREFIX})");
optPrefix.SetDefaultValue(PrometheusExporter.DEFAULT_PREFIX);

cmd.SetHandler((pveHost, pveValidateCertificate, pveUsername, pveApiToken, host, port, url, prefix) =>
{
    var exporter = new PrometheusExporter(pveHost,
                                          pveValidateCertificate,
                                          pveUsername,
                                          app.GetPasswordFromOption(),
                                          pveApiToken,
                                          loggerFactory,
                                          host,
                                          port,
                                          url,
                                          prefix);

    exporter.Start();

    Console.Out.WriteLine("Corsinvest for Proxmox VE");
    Console.Out.WriteLine($"Cluster: {pveHost} - User: {pveUsername}");
    Console.Out.WriteLine($"Exporter Prometheus: http://{host}:{port}/{url} - Prefix: {prefix}");

    Console.ReadLine();

    try { exporter.Stop(); }
    catch { }

    Console.Out.WriteLine("End application");
},
app.GetHostOption(),
app.GetValidateCertificateOption(),
app.GetUsernameOption(),
app.GetApiTokenOption(),
optHost,
optPort,
optUrl,
optPrefix);

return await app.ExecuteAppAsync(args, loggerFactory.CreateLogger(typeof(Program)));
