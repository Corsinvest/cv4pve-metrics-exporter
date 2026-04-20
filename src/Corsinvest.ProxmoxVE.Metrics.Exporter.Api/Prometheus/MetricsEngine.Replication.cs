/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private Gauge _replicationDuration = null!;
    private Gauge _replicationLastSync = null!;
    private Gauge _replicationNextSync = null!;
    private Counter _replicationFailed = null!;

    private void InitReplicationMetrics(MetricFactory mf)
    {
        var gaugeLabels = new GaugeConfiguration { LabelNames = ["id", "type", "source", "target", "guest"] };
        var counterLabels = new CounterConfiguration { LabelNames = ["id", "type", "source", "target", "guest"] };

        _replicationDuration = mf.CreateGauge("cv4pve_replication_duration_seconds", "Last replication duration", gaugeLabels);
        _replicationLastSync = mf.CreateGauge("cv4pve_replication_last_sync_timestamp_seconds", "Last successful sync (unix ts)", gaugeLabels);
        _replicationNextSync = mf.CreateGauge("cv4pve_replication_next_sync_timestamp_seconds", "Next scheduled sync (unix ts)", gaugeLabels);
        _replicationFailed = mf.CreateCounter("cv4pve_replication_failed_total", "Failed replication count", counterLabels);
    }

    private void WriteReplicationMetrics(IEnumerable<NodeReplication> jobs)
    {
        foreach (var j in jobs)
        {
            var labels = new[]
            {
                j.Id ?? "",
                j.Type ?? "",
                j.Source ?? "",
                j.Target ?? "",
                j.Guest
            };

            _replicationDuration.WithLabels(labels).Set(j.Duration);
            _replicationLastSync.WithLabels(labels).Set(j.LastSync);
            _replicationNextSync.WithLabels(labels).Set(j.NextSync);
            SetCounter(_replicationFailed, labels, j.FailCount);
        }
    }

    private static void SetCounter(Counter c, string[] labels, double value)
    {
        var current = c.WithLabels(labels).Value;
        if (value > current) { c.WithLabels(labels).Inc(value - current); }
    }
}
