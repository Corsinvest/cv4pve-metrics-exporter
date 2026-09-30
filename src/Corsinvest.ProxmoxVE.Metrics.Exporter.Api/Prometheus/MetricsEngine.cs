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

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

/// <summary>Collects Proxmox VE metrics and writes them into a Prometheus registry.</summary>
/// <remarks>
/// A collection that fails keeps the values of the last successful read; a successful read replaces them,
/// removing the series of objects that no longer exist. Not thread-safe: run one collection at a time.
/// </remarks>
public partial class MetricsEngine
{
    private readonly Settings _settings;
    private readonly ILogger<MetricsEngine> _logger;
    private readonly CollectorRegistry _registry;
    private readonly Dictionary<string, DateTime> _lastCollect = [];

    private IReadOnlyList<ClusterStatus> _statusEntries = [];
    private IReadOnlyList<ClusterResource> _resources = [];
    private int _failedCalls;

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

    /// <summary>True if the collector is on and its cache, if any, has expired.</summary>
    private bool ShouldCollect(string key, CollectorSettings cs)
    {
        if (!cs.Enabled) { return false; }
        if (cs.CacheSeconds <= 0) { return true; }

        lock (_lastCollect)
        {
            var last = _lastCollect.GetValueOrDefault(key, DateTime.MinValue);
            return DateTime.UtcNow - last >= TimeSpan.FromSeconds(cs.CacheSeconds);
        }
    }

    /// <summary>Starts the cache of a collector. Called only after a successful read, so a failed one is retried.</summary>
    private void MarkCollected(string key, CollectorSettings cs)
    {
        if (cs.CacheSeconds <= 0) { return; }
        lock (_lastCollect) { _lastCollect[key] = DateTime.UtcNow; }
    }

    /// <summary>Runs a full scrape: bulk cluster-wide fetch + per-node parallel + optional per-guest.</summary>
    public async Task CollectAsync(PveClient client)
    {
        var sw = Stopwatch.StartNew();
        _failedCalls = 0;

        try
        {
            await CollectClusterWideAsync(client);
            await CollectPerNodeAsync(client);
            if (ShouldCollect("guest:balloon", _settings.Guest.Balloon)) { await CollectBalloonAsync(client); }

            if (_failedCalls == 0) { _lastSuccessTimestamp.SetToCurrentTimeUtc(); }
        }
        finally
        {
            _scrapeDuration.Set(sw.Elapsed.TotalSeconds);
        }
    }

    private async Task CollectClusterWideAsync(PveClient client)
    {
        var statusTask = Fork(client).Cluster.Status.GetAsync();
        var resourcesTask = Fork(client).Cluster.Resources.GetAsync();
        var haEnabled = ShouldCollect("cluster:ha", _settings.Cluster.Ha);
        var backupEnabled = ShouldCollect("cluster:backup_info", _settings.Cluster.BackupInfo);

        var haTask = haEnabled ? Fork(client).Cluster.Ha.Resources.GetAsync() : null;
        var haManagerTask = haEnabled ? Checked(Fork(client).Cluster.Ha.Status.ManagerStatus.ManagerStatus()) : null;
        var backupTask = backupEnabled ? Checked(Fork(client).Cluster.BackupInfo.NotBackedUp.GetGuestsNotInBackup()) : null;

        var tasks = new Task?[] { statusTask, resourcesTask, haTask, haManagerTask, backupTask }
                        .Where(t => t is not null).Cast<Task>().ToArray();

        await SafeTaskExtensions.WhenAllSafe(tasks);
        TrackErrors("cluster", tasks);

        // A failed list keeps the previous one: the per-node calls still go to the nodes last seen online.
        if (statusTask.IsCompletedSuccessfully)
        {
            _statusEntries = [.. statusTask.Result];
            WriteStatusMetrics();
            WriteClusterMetrics();
        }

        if (resourcesTask.IsCompletedSuccessfully)
        {
            _resources = [.. resourcesTask.Result.CalculateHostUsage()];
            WriteResourceMetrics();
            if (_settings.Node.Status.Enabled) { WriteNodeAssignmentMetrics(); }
        }

        if (haTask?.IsCompletedSuccessfully == true && haManagerTask?.IsCompletedSuccessfully == true)
        {
            WriteHaMetrics(haTask.Result, haManagerTask.Result);
            MarkCollected("cluster:ha", _settings.Cluster.Ha);
        }

        if (backupTask?.IsCompletedSuccessfully == true)
        {
            WriteBackupMetrics(backupTask.Result);
            MarkCollected("cluster:backup_info", _settings.Cluster.BackupInfo);
        }
    }

    private IEnumerable<ClusterStatus> OnlineNodes
        => _statusEntries.Where(s => s.Type == "node" && s.IsOnline && !string.IsNullOrEmpty(s.Name));

    private async Task CollectPerNodeAsync(PveClient client)
    {
        var online = OnlineNodes.ToArray();
        await RunParallelAsync(online, node => CollectNodeAsync(client, node));

        // Nodes that are offline or gone: their per-node series are no longer current.
        var names = online.Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
        RemoveNodeSeries(labels => !names.Contains(labels[0]));
    }

    private async Task CollectNodeAsync(PveClient client, ClusterStatus node)
    {
        var name = node.Name;
        var statusEnabled = ShouldCollect($"node:status:{name}", _settings.Node.Status);
        var subEnabled = ShouldCollect($"node:subscription:{name}", _settings.Node.Subscription);
        var smartEnabled = ShouldCollect($"node:disk_smart:{name}", _settings.Node.DiskSmart);
        var replEnabled = ShouldCollect($"node:replication:{name}", _settings.Node.Replication);

        var statusTask = statusEnabled ? Fork(client).Nodes[name].Status.GetAsync() : null;
        var subTask = subEnabled ? Fork(client).Nodes[name].Subscription.GetAsync() : null;
        var versionTask = statusEnabled ? Fork(client).Nodes[name].Version.GetAsync() : null;
        var disksTask = smartEnabled ? Fork(client).Nodes[name].Disks.List.GetAsync() : null;
        var replTask = replEnabled ? Fork(client).Nodes[name].Replication.GetAsync() : null;

        var tasks = new Task?[] { statusTask, subTask, versionTask, disksTask, replTask }
                        .Where(t => t is not null).Cast<Task>().ToArray();
        if (tasks.Length == 0) { return; }

        await SafeTaskExtensions.WhenAllSafe(tasks);
        TrackErrors("node", tasks);

        if (statusTask?.IsCompletedSuccessfully == true) { WriteNodeStatusMetrics(node, statusTask.Result); }
        if (versionTask?.IsCompletedSuccessfully == true) { WriteNodeVersionMetrics(node, versionTask.Result); }
        if (statusTask?.IsCompletedSuccessfully == true && versionTask?.IsCompletedSuccessfully == true)
        {
            MarkCollected($"node:status:{name}", _settings.Node.Status);
        }

        if (subTask?.IsCompletedSuccessfully == true)
        {
            WriteNodeSubscriptionMetrics(node, subTask.Result);
            MarkCollected($"node:subscription:{name}", _settings.Node.Subscription);
        }

        if (disksTask?.IsCompletedSuccessfully == true)
        {
            WriteNodeDiskMetrics(node, disksTask.Result);
            MarkCollected($"node:disk_smart:{name}", _settings.Node.DiskSmart);
        }

        if (replTask?.IsCompletedSuccessfully == true)
        {
            WriteReplicationMetrics(node, replTask.Result);
            MarkCollected($"node:replication:{name}", _settings.Node.Replication);
        }
    }

    /// <summary>Removes the series of the per-node collectors whose node matches <paramref name="predicate"/>.</summary>
    private void RemoveNodeSeries(Func<string[], bool> predicate)
    {
        RemoveNodeStatusSeries(predicate);
        RemoveWhere(_nodeVersionInfo, predicate);
        RemoveNodeSubscriptionSeries(predicate);
        RemoveNodeDiskSeries(predicate);
        RemoveReplicationSeries(predicate);
    }

    private async Task RunParallelAsync<T>(IEnumerable<T> source, Func<T, Task> func)
    {
        using var semaphore = new SemaphoreSlim(_settings.MaxParallelRequests);
        await Task.WhenAll(source.Select(async item =>
        {
            await semaphore.WaitAsync();
            try { await func(item); }
            catch (Exception ex) { _logger.LogWarning(ex, "Parallel task failed"); }
            finally { semaphore.Release(); }
        }));
    }
}
