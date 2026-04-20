/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Api.Console.Helpers;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Metrics.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Settings = Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Settings;

const string SettingsFileName = "settings.json";

var app = ConsoleHelper.CreateApp("Metrics Exporter for Proxmox VE");

var optSettingsFile = app.AddOption<string>("--settings-file", $"Settings file (default: {SettingsFileName})")
                        .AddValidatorExistFile();

// create-settings
var cmdCreate = app.AddCommand("create-settings", $"Create settings file ({SettingsFileName})");
var optCreateFast = cmdCreate.AddOption<bool>("--fast", "Use fast profile");
var optCreateFull = cmdCreate.AddOption<bool>("--full", "Use full profile");
var optCreateOutput = cmdCreate.AddOption<string>("--output|-o", $"Output file path (default: {SettingsFileName})");

cmdCreate.SetAction((action) =>
{
    var settings = action.GetValue(optCreateFast) ? Settings.Fast()
                 : action.GetValue(optCreateFull) ? Settings.Full()
                 : Settings.Standard();

    var path = action.GetValue(optCreateOutput);
    if (string.IsNullOrWhiteSpace(path)) { path = SettingsFileName; }

    File.WriteAllText(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    Console.Out.WriteLine($"Created: {path}");
});

var cmdRun = app.AddCommand("run", "Run exporters");
var optRunFast = cmdRun.AddOption<bool>("--fast", "Use fast profile (ignored if --settings-file is set)");
var optRunFull = cmdRun.AddOption<bool>("--full", "Use full profile (ignored if --settings-file is set)");

cmdRun.SetAction(async (action) =>
{
    var settingsFile = action.GetValue(optSettingsFile);
    var settings = !string.IsNullOrWhiteSpace(settingsFile)
                        ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(settingsFile))!
                        : action.GetValue(optRunFast)
                            ? Settings.Fast()
                            : action.GetValue(optRunFull)
                                ? Settings.Full()
                                : Settings.Standard();

    var host = Host.CreateDefaultBuilder()
                   .UseSystemd()
                   .UseWindowsService()
                   .ConfigureLogging(logging =>
                   {
                       logging.ClearProviders();
                       logging.AddConsole();

                       var logLevel = app.GetLogLevelFromDebug();
                       logging.AddFilter("Microsoft", LogLevel.Warning);
                       logging.AddFilter("System", LogLevel.Warning);
                       logging.AddFilter("Corsinvest.ProxmoxVE.Api.PveClientBase", logLevel);
                       logging.SetMinimumLevel(logLevel);
                   })
                    .ConfigureServices((_, services) =>
                    {
                        var lf = LoggerFactory.Create(b => b.AddConsole());

                        services.AddSingleton(new MetricsServiceOptions
                        {
                            Settings = settings,
                            ClientFactory = () => ClientHelper.GetClientAndTryLoginAsync(
                                action.GetValue(app.GetHostOption())!,
                                action.GetValue(app.GetUsernameOption())!,
                                app.GetPasswordFromOption(),
                                action.GetValue(app.GetApiTokenOption()),
                                action.GetValue(app.GetValidateCertificateOption()),
                                lf),
                        });

                        services.AddHostedService<MetricsBackgroundService>();
                    })
                    .Build();

    await host.RunAsync();
});

var loggerFactory = ConsoleHelper.CreateLoggerFactory<Program>(app.GetLogLevelFromDebug());

return await app.ExecuteAppAsync(args, loggerFactory.CreateLogger<Program>());
