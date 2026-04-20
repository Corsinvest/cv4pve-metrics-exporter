/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Diagnostics;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Extensions;
using Microsoft.Extensions.Logging;
using Prometheus;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

/// <summary>Collects Proxmox VE metrics and writes them into a Prometheus registry.</summary>
public partial class MetricsEngine
{
    private readonly Settings _settings;
    private readonly ILogger<MetricsEngine> _logger;
    private readonly CollectorRegistry _registry;
    private readonly Dictionary<string, DateTime> _lastCollect = [];

    private IReadOnlyList<ClusterStatus> _statusEntries = [];
    private IReadOnlyList<ClusterResource> _resources = [];

    /// <summary>Creates the engine and initializes all metric definitions in the given registry.</summary>
    public MetricsEngine(Settings settings,
                         CollectorRegistry registry,
                         ILogger<MetricsEngine> logger)
    {
        _settings = settings;
        _registry = registry;
        _logger = logger;

        var mf = global::Prometheus.Metrics.WithCustomRegistry(registry);

        InitSelfMetrics(mf);
        InitStatusMetrics(mf);
        InitClusterMetrics(mf);
        InitResourceMetrics(mf);
        InitNodeStatusMetrics(mf);
        InitNodeSubscriptionMetrics(mf);
        InitNodeVersionMetrics(mf);
        InitNodeDiskMetrics(mf);
        InitReplicationMetrics(mf);
        InitHaMetrics(mf);
        InitBalloonMetrics(mf);
        InitBackupMetrics(mf);
        InitGuestLockMetrics(mf);
        if (settings.ApiInstrumentation) { InitApiInstrumentationMetrics(mf); }
    }

    private static double ToBit(bool v) => v ? 1 : 0;

    private bool ShouldCollect(string key, CollectorSettings cs)
    {
        if (!cs.Enabled) { return false; }
        if (cs.CacheSeconds <= 0) { return true; }

        var last = _lastCollect.GetValueOrDefault(key, DateTime.MinValue);
        if (DateTime.UtcNow - last < TimeSpan.FromSeconds(cs.CacheSeconds)) { return false; }

        _lastCollect[key] = DateTime.UtcNow;
        return true;
    }

    /// <summary>Runs a full scrape: bulk cluster-wide fetch + per-node parallel + optional per-guest.</summary>
    public async Task CollectAsync(PveClient client)
    {
        var sw = Stopwatch.StartNew();
        if (_settings.ApiInstrumentation) { client.RequestCompleted += OnApiRequestCompleted; }

        try
        {
            await CollectClusterWideAsync(client);
            await CollectPerNodeAsync(client);
            if (ShouldCollect("guest:balloon", _settings.Guest.Balloon)) { await CollectBalloonAsync(client); }

            _lastSuccessTimestamp.SetToCurrentTimeUtc();
        }
        finally
        {
            if (_settings.ApiInstrumentation) { client.RequestCompleted -= OnApiRequestCompleted; }
            _scrapeDuration.Set(sw.Elapsed.TotalSeconds);
        }
    }

    private async Task CollectClusterWideAsync(PveClient client)
    {
        var statusTask = client.Cluster.Status.GetAsync();
        var resourcesTask = client.Cluster.Resources.GetAsync();
        var haEnabled = ShouldCollect("cluster:ha", _settings.Cluster.Ha);
        var backupEnabled = ShouldCollect("cluster:backup_info", _settings.Cluster.BackupInfo);

        var haTask = haEnabled ? client.Cluster.Ha.Resources.GetAsync() : null;
        var haStatusTask = haEnabled ? client.Cluster.Ha.Status.Current.GetAsync() : null;
        var backupTask = backupEnabled ? client.Cluster.BackupInfo.NotBackedUp.GetGuestsNotInBackup() : null;

        var tasks = new Task?[] { statusTask, resourcesTask, haTask, haStatusTask, backupTask }
                        .Where(t => t is not null).Cast<Task>().ToArray();

        await SafeTaskExtensions.WhenAllSafe(tasks);
        TrackErrors("cluster", tasks);

        _statusEntries = [.. (statusTask.ResultOrDefault() ?? [])];
        _resources = [.. (resourcesTask.ResultOrDefault() ?? []).CalculateHostUsage()];

        WriteStatusMetrics();
        WriteClusterMetrics();
        WriteResourceMetrics();
        if (_settings.Node.Status.Enabled) { WriteNodeAssignmentMetrics(); }
        if (haTask?.ResultOrDefault() is { } ha) { WriteHaMetrics(ha); }
        if (haStatusTask?.ResultOrDefault() is { } haStatus) { WriteHaStatusMetrics(haStatus); }
        if (backupTask?.ResultOrDefault() is { } backup) { WriteBackupMetrics(backup); }
    }

    private Task CollectPerNodeAsync(PveClient client)
        => RunParallelAsync(_statusEntries.Where(s => s.Type == "node" && s.IsOnline && !string.IsNullOrEmpty(s.Name)),
                            node => CollectNodeAsync(client, node));

    private async Task CollectNodeAsync(PveClient client, ClusterStatus node)
    {
        var statusEnabled = ShouldCollect($"node:status:{node.Name}", _settings.Node.Status);
        var subEnabled = ShouldCollect($"node:subscription:{node.Name}", _settings.Node.Subscription);
        var smartEnabled = ShouldCollect($"node:disk_smart:{node.Name}", _settings.Node.DiskSmart);
        var replEnabled = ShouldCollect($"node:replication:{node.Name}", _settings.Node.Replication);

        var statusTask = statusEnabled ? client.Nodes[node.Name].Status.GetAsync() : null;
        var subTask = subEnabled ? client.Nodes[node.Name].Subscription.GetAsync() : null;
        var versionTask = statusEnabled ? client.Nodes[node.Name].Version.GetAsync() : null;
        var disksTask = smartEnabled ? client.Nodes[node.Name].Disks.List.GetAsync() : null;
        var replTask = replEnabled ? client.Nodes[node.Name].Replication.GetAsync() : null;

        var tasks = new Task?[] { statusTask, subTask, versionTask, disksTask, replTask }
                        .Where(t => t is not null).Cast<Task>().ToArray();
        if (tasks.Length == 0) { return; }

        await SafeTaskExtensions.WhenAllSafe(tasks);
        TrackErrors("node", tasks);

        if (statusTask?.ResultOrDefault() is { } st) { WriteNodeStatusMetrics(node, st); }
        if (subTask?.ResultOrDefault() is { } sb) { WriteNodeSubscriptionMetrics(node, sb); }
        if (versionTask?.ResultOrDefault() is { } vr) { WriteNodeVersionMetrics(node, vr); }
        if (disksTask?.ResultOrDefault() is { } dk) { WriteNodeDiskMetrics(node, dk); }
        if (replTask?.ResultOrDefault() is { } rp) { WriteReplicationMetrics(rp); }
    }

    private async Task RunParallelAsync<T>(IEnumerable<T> source, Func<T, Task> func)
    {
        var semaphore = new SemaphoreSlim(_settings.MaxParallelRequests);
        await Task.WhenAll(source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try { await func(item); }
            catch (Exception ex) { _logger.LogWarning(ex, "Parallel task failed"); }
            finally { semaphore.Release(); }
        }));
    }
}
