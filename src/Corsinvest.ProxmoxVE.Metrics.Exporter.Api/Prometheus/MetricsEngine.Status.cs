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

    private void WriteStatusMetrics()
    {
        foreach (var item in _statusEntries)
        {
            if (item.Type == "node" && !string.IsNullOrEmpty(item.Id))
            {
                _up.WithLabels(item.Id, "node").Set(ToBit(item.IsOnline));
            }
            else if (item.Type == "cluster" && !string.IsNullOrEmpty(item.Name))
            {
                _up.WithLabels($"cluster/{item.Name}", "cluster").Set(item.Quorate);
            }
        }

        foreach (var item in _resources.Where(r => !string.IsNullOrEmpty(r.Id)))
        {
            switch (item.ResourceType)
            {
                case ClusterResourceType.Vm:
                    _up.WithLabels(item.Id, item.VmType.ToString().ToLowerInvariant())
                       .Set(ToBit(item.IsRunning));
                    break;

                case ClusterResourceType.Storage:
                    _up.WithLabels(item.Id, "storage").Set(ToBit(item.IsAvailable));
                    break;
            }
        }
    }
}
