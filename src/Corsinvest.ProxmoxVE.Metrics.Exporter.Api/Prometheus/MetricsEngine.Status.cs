/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private Gauge _up = null!;

    private void InitStatusMetrics(MetricFactory mf)
        => _up = mf.CreateGauge("cv4pve_up",
                                "Resource is online/running/available (1) or not (0)",
                                new GaugeConfiguration { LabelNames = ["id", "type"] });

    /// <summary>cv4pve_up of the cluster and the nodes, from /cluster/status.</summary>
    private void WriteStatusMetrics()
    {
        var series = new Series();
        foreach (var item in _statusEntries)
        {
            if (item.Type == "node" && !string.IsNullOrEmpty(item.Id))
            {
                series.Set(_up, ToBit(item.IsOnline), item.Id, "node");
            }
            else if (item.Type == "cluster" && !string.IsNullOrEmpty(item.Name))
            {
                series.Set(_up, item.Quorate, $"cluster/{item.Name}", "cluster");
            }
        }
        series.Prune(_up, labels => labels[1] is "node" or "cluster");
    }

    /// <summary>cv4pve_up of guests and storages, from /cluster/resources.</summary>
    private void WriteResourceStatusMetrics(Series series)
    {
        foreach (var item in _resources.Where(r => !string.IsNullOrEmpty(r.Id)))
        {
            switch (item.ResourceType)
            {
                case ClusterResourceType.Vm:
                    series.Set(_up, ToBit(item.IsRunning), item.Id, item.VmType.ToString().ToLowerInvariant());
                    break;

                case ClusterResourceType.Storage:
                    series.Set(_up, ToBit(item.IsAvailable), item.Id, "storage");
                    break;
            }
        }
        series.Prune(_up, labels => labels[1] is not "node" and not "cluster");
    }
}
