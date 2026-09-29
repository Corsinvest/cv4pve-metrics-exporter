/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Text.Json;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Tests;

/// <summary>The three profiles, as documented in docs/settings.mdx.</summary>
public class SettingsTests
{
    private static string Json(Settings settings) => JsonSerializer.Serialize(settings);

    [Fact]
    public void Standard_profile_caches_backup_coverage_and_subscription()
    {
        var p = Settings.Standard().Prometheus;

        Assert.True(p.ApiInstrumentation);
        Assert.Equal((true, 0), (p.Cluster.Ha.Enabled, p.Cluster.Ha.CacheSeconds));
        Assert.Equal((true, 600), (p.Cluster.BackupInfo.Enabled, p.Cluster.BackupInfo.CacheSeconds));
        Assert.Equal((true, 0), (p.Node.Status.Enabled, p.Node.Status.CacheSeconds));
        Assert.Equal((true, 3600), (p.Node.Subscription.Enabled, p.Node.Subscription.CacheSeconds));
        Assert.Equal((true, 0), (p.Node.Replication.Enabled, p.Node.Replication.CacheSeconds));
        Assert.False(p.Node.DiskSmart.Enabled);
        Assert.False(p.Guest.Balloon.Enabled);
    }

    [Fact]
    public void Fast_profile_has_no_per_node_collector_and_no_cache()
    {
        var p = Settings.Fast().Prometheus;

        Assert.False(p.ApiInstrumentation);
        Assert.Equal((true, 0), (p.Cluster.Ha.Enabled, p.Cluster.Ha.CacheSeconds));
        Assert.Equal((true, 0), (p.Cluster.BackupInfo.Enabled, p.Cluster.BackupInfo.CacheSeconds));
        Assert.False(p.Node.Status.Enabled);
        Assert.False(p.Node.Subscription.Enabled);
        Assert.False(p.Node.Replication.Enabled);
        Assert.False(p.Node.DiskSmart.Enabled);
        Assert.False(p.Guest.Balloon.Enabled);
    }

    [Fact]
    public void Full_profile_turns_everything_on_with_cache()
    {
        var p = Settings.Full().Prometheus;

        Assert.True(p.ApiInstrumentation);
        Assert.Equal((true, 30), (p.Cluster.Ha.Enabled, p.Cluster.Ha.CacheSeconds));
        Assert.Equal((true, 600), (p.Cluster.BackupInfo.Enabled, p.Cluster.BackupInfo.CacheSeconds));
        Assert.Equal((true, 0), (p.Node.Status.Enabled, p.Node.Status.CacheSeconds));
        Assert.Equal((true, 3600), (p.Node.Subscription.Enabled, p.Node.Subscription.CacheSeconds));
        Assert.Equal((true, 60), (p.Node.Replication.Enabled, p.Node.Replication.CacheSeconds));
        Assert.Equal((true, 600), (p.Node.DiskSmart.Enabled, p.Node.DiskSmart.CacheSeconds));
        Assert.Equal((true, 0), (p.Guest.Balloon.Enabled, p.Guest.Balloon.CacheSeconds));
    }

    [Fact]
    public void Profiles_share_the_endpoint_and_parallelism()
    {
        foreach (var p in new[] { Settings.Fast().Prometheus, Settings.Standard().Prometheus, Settings.Full().Prometheus })
        {
            Assert.True(p.Enabled);
            Assert.Equal(("localhost", 9221, "metrics/", 5), (p.Host, p.Port, p.Url, p.MaxParallelRequests));
        }
    }

    [Fact]
    public void A_setting_left_out_of_the_file_takes_the_standard_default()
    {
        var fromEmptyFile = JsonSerializer.Deserialize<Settings>("""{ "Prometheus": { "Port": 9300 } }""")!;
        var expected = Settings.Standard();
        expected.Prometheus.Port = 9300;

        Assert.Equal(Json(expected), Json(fromEmptyFile));
        Assert.Equal(Json(Settings.Standard()), Json(JsonSerializer.Deserialize<Settings>("{}")!));
    }

    [Fact]
    public void Written_profile_reads_back_the_same()
    {
        foreach (var s in new[] { Settings.Fast(), Settings.Standard(), Settings.Full() })
        {
            Assert.Equal(Json(s), Json(JsonSerializer.Deserialize<Settings>(Json(s))!));
        }
    }
}
