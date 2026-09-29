/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Net;
using System.Text.Json.Nodes;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Tests.Helpers;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Tests;

/// <summary>What each collection writes, against an in-memory Proxmox VE API.</summary>
public class MetricsEngineTests
{
    private static EngineHarness AllOn() => new(EngineHarness.AllOn());

    // ----- Values -----

    [Fact]
    public async Task Writes_cluster_node_guest_and_storage_metrics()
    {
        var s = await AllOn().CollectAsync();

        Assert.Equal(1, s.Value("cv4pve_up", "id=cluster/pve-cluster", "type=cluster"));
        Assert.Equal(1, s.Value("cv4pve_up", "id=node/pve01", "type=node"));
        Assert.Equal(1, s.Value("cv4pve_up", "id=qemu/100", "type=qemu"));
        Assert.Equal(0, s.Value("cv4pve_up", "id=qemu/200", "type=qemu"));
        Assert.Equal(1, s.Value("cv4pve_up", "id=storage/pve01/local", "type=storage"));
        Assert.Equal(1, s.Value("cv4pve_cluster_quorate", "name=pve-cluster"));
        Assert.Equal(2, s.Value("cv4pve_cluster_nodes", "name=pve-cluster"));
        Assert.Equal(1, s.Value("cv4pve_node_info", "id=node/pve01", "name=pve01", "ip=10.0.0.11", "level=Community"));
        Assert.Equal(0.25, s.Value("cv4pve_guest_cpu_usage_ratio", "id=qemu/100"));
        Assert.Equal(4, s.Value("cv4pve_guest_cpu_cores", "id=qemu/100"));
        Assert.Equal(4294967296, s.Value("cv4pve_guest_memory_usage_bytes", "id=qemu/100"));
        Assert.Equal(1, s.Value("cv4pve_storage_shared", "id=storage/pve02/nfs01"));
        Assert.Equal(1, s.Value("cv4pve_storage_info", "id=storage/pve01/local", "content=backup,iso,vztmpl"));
    }

    [Fact]
    public async Task Guest_tags_are_sorted()
    {
        var s = await AllOn().CollectAsync();

        Assert.True(s.Has("cv4pve_guest_info", "id=qemu/100", "tags=prod;web", "template=0", "type=qemu"));
    }

    [Fact]
    public async Task Guest_lock_has_one_series_per_state()
    {
        var h = AllOn();
        h.Pve.Resource("qemu/100")["lock"] = "backup";
        var s = await h.CollectAsync();

        Assert.Equal(9, s.Find("cv4pve_guest_lock", "id=qemu/100").Count);
        Assert.Equal(1, s.Value("cv4pve_guest_lock", "id=qemu/100", "state=backup"));
        Assert.Equal(0, s.Value("cv4pve_guest_lock", "id=qemu/100", "state=snapshot"));
    }

    [Fact]
    public async Task Overcommit_sums_running_guests_of_the_node()
    {
        var s = await AllOn().CollectAsync();

        Assert.Equal(6, s.Value("cv4pve_node_cpu_assigned_cores", "node=pve01"));
        Assert.Equal(8589934592 + 4294967296, s.Value("cv4pve_node_memory_assigned_bytes", "node=pve01"));
        Assert.Equal(0, s.Value("cv4pve_node_cpu_assigned_cores", "node=pve02"));
    }

    [Fact]
    public async Task Node_status_version_and_subscription()
    {
        var s = await AllOn().CollectAsync();

        Assert.Equal(0.5, s.Value("cv4pve_node_load_avg1", "node=pve01"));
        Assert.Equal(1, s.Value("cv4pve_node_version_info", "node=pve01", "version=9.0.10", "release=9.0"));
        Assert.Equal(1, s.Value("cv4pve_node_subscription_status", "node=pve01", "status=active"));
        Assert.Equal(0, s.Value("cv4pve_node_subscription_status", "node=pve01", "status=expired"));
        Assert.Equal(new DateTimeOffset(2027, 1, 31, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
                     s.Value("cv4pve_node_subscription_next_due_timestamp_seconds", "node=pve01"));
    }

    [Fact]
    public async Task Balloon_is_read_from_the_qemu_monitor()
    {
        var s = await AllOn().CollectAsync();

        Assert.Equal(6144d * 1024 * 1024, s.Value("cv4pve_guest_balloon_actual_bytes", "id=qemu/100"));
    }

    // ----- SMART -----

    [Theory]
    [InlineData("PASSED", 1)] // ATA, NVMe
    [InlineData("OK", 1)]     // SAS
    [InlineData("FAILED", 0)]
    [InlineData("UNKNOWN", 0)]
    public async Task Smart_health_is_good_when_passed_or_ok(string health, double expected)
    {
        var h = AllOn();
        h.Pve.Data["/nodes/pve01/disks/list"]![0]!["health"] = health;
        var s = await h.CollectAsync();

        Assert.Equal(expected, s.Value("cv4pve_node_disk_health", "node=pve01", "serial=SSD0001"));
    }

    [Fact]
    public async Task Wearout_is_not_exported_when_not_available()
    {
        var h = AllOn();
        h.Pve.Data["/nodes/pve01/disks/list"]![0]!["wearout"] = "N/A";
        var s = await h.CollectAsync();

        Assert.False(s.Has("cv4pve_node_disk_wearout", "node=pve01"));
        Assert.Equal(97, s.Value("cv4pve_node_disk_wearout", "node=pve02"));
    }

    // ----- Objects that disappear -----

    [Fact]
    public async Task Deleted_guest_is_no_longer_exported()
    {
        var h = AllOn();
        await h.CollectAsync();
        h.Pve.RemoveResource("qemu/200");
        var s = await h.CollectAsync();

        Assert.False(s.Has("cv4pve_guest_info", "id=qemu/200"));
        Assert.False(s.Has("cv4pve_up", "id=qemu/200"));
        Assert.False(s.Has("cv4pve_guest_lock", "id=qemu/200"));
        Assert.False(s.Has("cv4pve_guest_memory_size_bytes", "id=qemu/200"));
        Assert.False(s.Has("cv4pve_guest_network_receive_bytes_total", "id=qemu/200"));
    }

    [Fact]
    public async Task Renamed_guest_keeps_only_the_new_info_series()
    {
        var h = AllOn();
        await h.CollectAsync();
        h.Pve.Resource("qemu/100")["name"] = "web02";
        h.Pve.Resource("qemu/100")["node"] = "pve02";
        var s = await h.CollectAsync();

        Assert.Equal("web02", Assert.Single(s.Find("cv4pve_guest_info", "id=qemu/100")).Labels["name"]);
        Assert.Equal("pve02", s.Find("cv4pve_guest_info", "id=qemu/100")[0].Labels["node"]);
    }

    [Fact]
    public async Task Removed_storage_is_no_longer_exported()
    {
        var h = AllOn();
        await h.CollectAsync();
        h.Pve.RemoveResource("storage/pve01/local");
        var s = await h.CollectAsync();

        Assert.False(s.Has("cv4pve_storage_info", "id=storage/pve01/local"));
        Assert.False(s.Has("cv4pve_storage_usage_bytes", "id=storage/pve01/local"));
        Assert.False(s.Has("cv4pve_up", "id=storage/pve01/local"));
    }

    [Fact]
    public async Task Guest_covered_by_a_new_backup_job_disappears_from_the_list()
    {
        var h = AllOn();
        h.Pve.Data["/cluster/backup-info/not-backed-up"] = JsonNode.Parse("""[ { "type": "qemu", "vmid": 200, "name": "test01" } ]""");
        var s = await h.CollectAsync();
        Assert.Equal(1, s.Value("cv4pve_not_backed_up_info", "id=qemu/200"));
        Assert.Equal(1, s.Value("cv4pve_guests_not_backed_up"));

        h.Pve.Data["/cluster/backup-info/not-backed-up"] = new JsonArray();
        s = await h.CollectAsync();

        Assert.False(s.Has("cv4pve_not_backed_up_info"));
        Assert.Equal(0, s.Value("cv4pve_guests_not_backed_up"));
    }

    [Fact]
    public async Task Upgraded_node_keeps_only_the_new_version()
    {
        var h = AllOn();
        await h.CollectAsync();
        h.Pve.Data["/nodes/pve01/version"]!["version"] = "9.0.11";
        var s = await h.CollectAsync();

        Assert.Equal("9.0.11", Assert.Single(s.Find("cv4pve_node_version_info", "node=pve01")).Labels["version"]);
    }

    [Fact]
    public async Task Offline_node_loses_its_per_node_metrics_but_keeps_info_and_up()
    {
        var h = AllOn();
        await h.CollectAsync();
        h.Pve.Status[2]!["online"] = 0;
        h.Pve.Resource("node/pve02")["status"] = "offline";
        var s = await h.CollectAsync();

        Assert.Equal(0, s.Value("cv4pve_up", "id=node/pve02"));
        Assert.True(s.Has("cv4pve_node_info", "id=node/pve02"));
        Assert.False(s.Has("cv4pve_node_uptime_seconds", "node=pve02"));
        Assert.False(s.Has("cv4pve_node_version_info", "node=pve02"));
        Assert.False(s.Has("cv4pve_node_subscription_status", "node=pve02"));
        Assert.False(s.Has("cv4pve_node_disk_health", "node=pve02"));
        Assert.True(s.Has("cv4pve_node_uptime_seconds", "node=pve01"));
    }

    [Fact]
    public async Task Deleted_replication_job_is_no_longer_exported()
    {
        var h = AllOn();
        await h.CollectAsync();
        h.Pve.Data["/nodes/pve01/replication"] = new JsonArray();
        var s = await h.CollectAsync();

        Assert.False(s.Has("cv4pve_replication_last_sync_timestamp_seconds"));
        Assert.False(s.Has("cv4pve_replication_fail_count"));
    }

    // ----- Counters -----

    [Fact]
    public async Task Guest_counters_reset_when_proxmox_restarts_counting()
    {
        var h = AllOn();
        h.Pve.Resource("qemu/100")["netin"] = 5000;
        await h.CollectAsync();
        h.Pve.Resource("qemu/100")["netin"] = 300; // the VM restarted
        var s = await h.CollectAsync();

        Assert.Equal(300, s.Value("cv4pve_guest_network_receive_bytes_total", "id=qemu/100"));
    }

    [Fact]
    public async Task Guest_counters_follow_the_value_while_it_grows()
    {
        var h = AllOn();
        await h.CollectAsync();
        h.Pve.Resource("qemu/100")["diskwrite"] = 9000;
        var s = await h.CollectAsync();

        Assert.Equal(9000, s.Value("cv4pve_guest_disk_write_bytes_total", "id=qemu/100"));
    }

    [Fact]
    public async Task Replication_fail_count_is_a_gauge_that_goes_back_to_zero()
    {
        var h = AllOn();
        h.Pve.Data["/nodes/pve01/replication"]![0]!["fail_count"] = 3;
        var s = await h.CollectAsync();
        Assert.Equal(3, s.Value("cv4pve_replication_fail_count", "id=100-0", "source=pve01", "target=pve02", "guest=100"));
        Assert.Contains("cv4pve_replication_fail_count gauge", s.Types);
        Assert.DoesNotContain(s.Types, t => t.StartsWith("cv4pve_replication_failed_total"));

        h.Pve.Data["/nodes/pve01/replication"]![0]!["fail_count"] = 0;
        s = await h.CollectAsync();
        Assert.Equal(0, s.Value("cv4pve_replication_fail_count", "id=100-0"));
    }

    // ----- HA -----

    private static void AddHa(FakePve pve)
    {
        pve.Data["/cluster/ha/resources"] = JsonNode.Parse("""
            [ { "sid": "vm:100", "type": "vm", "state": "started", "group": "prefer-pve01" },
              { "sid": "ct:101", "type": "ct", "state": "started" } ]
            """);
        pve.Data["/cluster/ha/status/manager_status"] = JsonNode.Parse("""
            {
              "manager_status": {
                "master_node": "pve01",
                "node_status": { "pve01": "online", "pve02": "maintenance" },
                "service_status": {
                  "vm:100": { "node": "pve01", "state": "started", "running": 1 },
                  "ct:101": { "node": "pve01", "state": "error" }
                }
              },
              "quorum": { "node": "pve01", "quorate": 1 }
            }
            """);
    }

    [Fact]
    public async Task Ha_resource_state_comes_from_the_ha_manager()
    {
        var h = AllOn();
        AddHa(h.Pve);
        var s = await h.CollectAsync();

        Assert.Equal(1, s.Value("cv4pve_ha_state", "sid=vm:100", "type=vm", "group=prefer-pve01", "state=started"));
        Assert.Equal(1, s.Value("cv4pve_ha_state", "sid=ct:101", "type=ct", "group=", "state=error"));
        Assert.Equal(0, s.Value("cv4pve_ha_state", "sid=ct:101", "state=started"));
        Assert.Equal(11, s.Find("cv4pve_ha_state", "sid=vm:100").Count);
    }

    [Fact]
    public async Task Ha_node_state_comes_from_the_ha_manager()
    {
        var h = AllOn();
        AddHa(h.Pve);
        var s = await h.CollectAsync();

        Assert.Equal(1, s.Value("cv4pve_ha_node_state", "node=pve01", "state=online"));
        Assert.Equal(1, s.Value("cv4pve_ha_node_state", "node=pve02", "state=maintenance"));
        Assert.Equal(0, s.Value("cv4pve_ha_node_state", "node=pve02", "state=online"));
        Assert.Equal(5, s.Find("cv4pve_ha_node_state", "node=pve01").Count);
        Assert.False(s.Has("cv4pve_ha_node_state", "node="));
        Assert.Equal(1, s.Value("cv4pve_ha_quorate"));
    }

    [Fact]
    public async Task Cluster_without_ha_has_no_ha_series_except_quorum()
    {
        var s = await AllOn().CollectAsync();

        Assert.False(s.Has("cv4pve_ha_state"));
        Assert.False(s.Has("cv4pve_ha_node_state"));
        Assert.Equal(1, s.Value("cv4pve_ha_quorate"));
    }

    [Fact]
    public async Task Removed_ha_resource_is_no_longer_exported()
    {
        var h = AllOn();
        AddHa(h.Pve);
        await h.CollectAsync();
        h.Pve.Data["/cluster/ha/resources"]!.AsArray().RemoveAt(1);
        var s = await h.CollectAsync();

        Assert.False(s.Has("cv4pve_ha_state", "sid=ct:101"));
        Assert.True(s.Has("cv4pve_ha_state", "sid=vm:100"));
    }

    // ----- Failures -----

    [Fact]
    public async Task Failed_call_keeps_previous_values_and_counts_the_error()
    {
        var h = AllOn();
        await h.CollectAsync();
        h.Pve.Failures["/nodes/pve01/status"] = HttpStatusCode.InternalServerError;
        var s = await h.CollectAsync();

        Assert.Equal(0.5, s.Value("cv4pve_node_load_avg1", "node=pve01"));
        Assert.Equal(1, s.Value("cv4pve_scrape_errors_total", "section=node"));
    }

    [Theory]
    [InlineData("/cluster/backup-info/not-backed-up", "cluster")]
    [InlineData("/cluster/ha/status/manager_status", "cluster")]
    [InlineData("/nodes/pve01/qemu/100/monitor", "guest")]
    public async Task Every_failed_call_is_counted(string path, string section)
    {
        var h = AllOn();
        h.Pve.Failures[path] = HttpStatusCode.InternalServerError;
        var s = await h.CollectAsync();

        Assert.Equal(1, s.Value("cv4pve_scrape_errors_total", $"section={section}"));
    }

    [Fact]
    public async Task Last_success_advances_only_when_every_call_succeeds()
    {
        var h = AllOn();
        var s = await h.CollectAsync();
        var first = s.Value("cv4pve_scrape_last_success_timestamp_seconds");
        Assert.True(first > 0);

        await Task.Delay(20);
        h.Pve.Failures["/nodes/pve02/subscription"] = HttpStatusCode.InternalServerError;
        s = await h.CollectAsync();

        Assert.Equal(first, s.Value("cv4pve_scrape_last_success_timestamp_seconds"));
    }

    // ----- Cache -----

    [Fact]
    public async Task Cached_collector_is_not_called_again_and_keeps_its_values()
    {
        var settings = EngineHarness.AllOn();
        settings.Node.Subscription.CacheSeconds = 3600;
        var h = new EngineHarness(settings);
        await h.CollectAsync();
        h.Pve.Calls.Clear();
        var s = await h.CollectAsync();

        Assert.DoesNotContain("GET /nodes/pve01/subscription", h.Pve.Calls);
        Assert.Contains("GET /nodes/pve01/status", h.Pve.Calls);
        Assert.Equal(1, s.Value("cv4pve_node_subscription_status", "node=pve01", "status=active"));
    }

    [Fact]
    public async Task Failed_call_is_not_cached()
    {
        var settings = EngineHarness.AllOn();
        settings.Node.Subscription.CacheSeconds = 3600;
        var h = new EngineHarness(settings);
        h.Pve.Failures["/nodes/pve01/subscription"] = HttpStatusCode.InternalServerError;
        await h.CollectAsync();

        h.Pve.Failures.Clear();
        h.Pve.Calls.Clear();
        var s = await h.CollectAsync();

        Assert.Contains("GET /nodes/pve01/subscription", h.Pve.Calls);
        Assert.Equal(1, s.Value("cv4pve_node_subscription_status", "node=pve01", "status=active"));
    }

    // ----- Concurrent calls -----

    [Fact]
    public async Task Concurrent_calls_get_their_own_results()
    {
        // PveClientBase (SDK 9.2.3) returns its shared LastResult field: with a slow RequestCompleted handler,
        // concurrent calls on one client receive each other's answers.
        var h = AllOn();
        h.Pve.Latency = TimeSpan.FromMilliseconds(10);
        var client = h.Pve.CreateClient();
        client.RequestCompleted += (_, _) => Thread.Sleep(20);

        for (var i = 0; i < 5; i++)
        {
            await h.Engine.CollectAsync(client);
            var s = await Scrape.FromAsync(h.Registry);

            Assert.False(s.Has("cv4pve_scrape_errors_total"), $"collection {i} had failed calls");
            Assert.Equal(1, s.Value("cv4pve_up", "id=node/pve02"));
            Assert.Equal(0.5, s.Value("cv4pve_guest_cpu_usage_ratio", "id=lxc/101"));
        }
    }

    // ----- Collectors off -----

    [Fact]
    public async Task Fast_profile_makes_no_per_node_call()
    {
        var h = new EngineHarness(Api.Prometheus.Settings.Fast());
        await h.CollectAsync();

        Assert.DoesNotContain(h.Pve.Calls, c => c.Contains("/nodes/"));
        Assert.Contains("GET /cluster/ha/resources", h.Pve.Calls);
        Assert.Contains("GET /cluster/backup-info/not-backed-up", h.Pve.Calls);
    }
}
