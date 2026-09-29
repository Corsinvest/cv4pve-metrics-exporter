/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Globalization;
using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;
using Microsoft.Extensions.Logging.Abstractions;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Tests.Helpers;

/// <summary>A <see cref="MetricsEngine"/> on its own registry, reading a <see cref="FakePve"/>.</summary>
internal sealed class EngineHarness
{
    public FakePve Pve { get; }
    public CollectorRegistry Registry { get; } = Prometheus.Metrics.NewCustomRegistry();
    public MetricsEngine Engine { get; }

    public EngineHarness(Settings settings, FakePve? pve = null)
    {
        Pve = pve ?? FakePve.TwoNodeCluster();
        Engine = new MetricsEngine(settings, Registry, NullLogger<MetricsEngine>.Instance);
    }

    /// <summary>Every collector on, no cache: each collection reads everything.</summary>
    public static Settings AllOn()
    {
        var settings = Settings.Full();
        foreach (var cs in new[] { settings.Cluster.Ha, settings.Cluster.BackupInfo, settings.Node.Status,
                                   settings.Node.Subscription, settings.Node.Replication, settings.Node.DiskSmart,
                                   settings.Guest.Balloon })
        {
            cs.Enabled = true;
            cs.CacheSeconds = 0;
        }
        return settings;
    }

    public async Task<Scrape> CollectAsync()
    {
        await Engine.CollectAsync(Pve.CreateClient());
        return await Scrape.FromAsync(Registry);
    }
}

/// <summary>The text exposition of a registry, parsed into series and values.</summary>
internal sealed partial class Scrape
{
    [GeneratedRegex(@"^(?<name>[a-zA-Z_:][a-zA-Z0-9_:]*)(\{(?<labels>.*)\})? (?<value>\S+)$")]
    private static partial Regex SeriesRegex();

    [GeneratedRegex(@"(?<k>[a-zA-Z_][a-zA-Z0-9_]*)=""(?<v>(?:[^""\\]|\\.)*)""")]
    private static partial Regex LabelRegex();

    public List<(string Name, Dictionary<string, string> Labels, double Value)> Series { get; } = [];
    public HashSet<string> Types { get; } = [];

    public static async Task<Scrape> FromAsync(CollectorRegistry registry)
    {
        using var stream = new MemoryStream();
        await registry.CollectAndExportAsTextAsync(stream);
        var scrape = new Scrape();
        foreach (var line in System.Text.Encoding.UTF8.GetString(stream.ToArray()).Split('\n'))
        {
            if (line.StartsWith("# TYPE ")) { scrape.Types.Add(line["# TYPE ".Length..]); continue; }
            if (line.StartsWith('#') || line.Length == 0) { continue; }

            var m = SeriesRegex().Match(line);
            var labels = LabelRegex().Matches(m.Groups["labels"].Value).ToDictionary(a => a.Groups["k"].Value, a => a.Groups["v"].Value);
            scrape.Series.Add((m.Groups["name"].Value, labels, double.Parse(m.Groups["value"].Value, CultureInfo.InvariantCulture)));
        }
        return scrape;
    }

    /// <summary>Series of a metric whose labels include all of <paramref name="labels"/> (<c>"key=value"</c>).</summary>
    public List<(string Name, Dictionary<string, string> Labels, double Value)> Find(string name, params string[] labels)
        => [.. Series.Where(s => s.Name == name
                                 && labels.All(l => l.Split('=', 2) is [var k, var v] && s.Labels.GetValueOrDefault(k) == v))];

    /// <summary>Value of the single series matching; fails if there is none or more than one.</summary>
    public double Value(string name, params string[] labels) => Assert.Single(Find(name, labels)).Value;

    public bool Has(string name, params string[] labels) => Find(name, labels).Count > 0;
}
