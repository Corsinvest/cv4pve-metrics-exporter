/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private const int ReplicationSourceLabel = 2;

    private Gauge _replicationDuration = null!;
    private Gauge _replicationLastSync = null!;
    private Gauge _replicationNextSync = null!;
    private Gauge _replicationFailCount = null!;

    private void InitReplicationMetrics(MetricFactory mf)
    {
        var labels = new GaugeConfiguration { LabelNames = ["id", "type", "source", "target", "guest"] };

        _replicationDuration = mf.CreateGauge("cv4pve_replication_duration_seconds", "Last replication duration", labels);
        _replicationLastSync = mf.CreateGauge("cv4pve_replication_last_sync_timestamp_seconds", "Last successful sync (unix ts)", labels);
        _replicationNextSync = mf.CreateGauge("cv4pve_replication_next_sync_timestamp_seconds", "Next scheduled sync (unix ts)", labels);

        // Proxmox VE counts the failed attempts in a row and goes back to 0 at the first success: a gauge, not a counter.
        _replicationFailCount = mf.CreateGauge("cv4pve_replication_fail_count", "Failed replication attempts in a row (0 after a successful sync)", labels);
    }

    /// <summary>The jobs listed by a node are those it runs, i.e. whose source is that node.</summary>
    private void WriteReplicationMetrics(ClusterStatus node, IEnumerable<NodeReplication> jobs)
    {
        var series = new Series();
        foreach (var j in jobs)
        {
            var labels = new[]
            {
                j.Id ?? "",
                j.Type ?? "",
                j.Source ?? "",
                j.Target ?? "",
                j.Guest ?? ""
            };

            series.Set(_replicationDuration, j.Duration, labels);
            series.Set(_replicationLastSync, j.LastSync, labels);
            series.Set(_replicationNextSync, j.NextSync, labels);
            series.Set(_replicationFailCount, j.FailCount, labels);
        }

        bool OfNode(string[] labels) => labels[ReplicationSourceLabel] == node.Name;
        foreach (var gauge in new[] { _replicationDuration, _replicationLastSync, _replicationNextSync, _replicationFailCount })
        {
            series.Prune(gauge, OfNode);
        }
    }

    /// <summary><paramref name="predicate"/> receives the source node as first label, like the other per-node metrics.</summary>
    private void RemoveReplicationSeries(Func<string[], bool> predicate)
    {
        foreach (var gauge in new[] { _replicationDuration, _replicationLastSync, _replicationNextSync, _replicationFailCount })
        {
            RemoveWhere(gauge, labels => predicate([labels[ReplicationSourceLabel]]));
        }
    }
}
