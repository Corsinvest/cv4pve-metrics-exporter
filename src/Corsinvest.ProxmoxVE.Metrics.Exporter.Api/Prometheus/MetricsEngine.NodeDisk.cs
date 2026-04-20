/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Globalization;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private Gauge _nodeDiskHealth = null!;
    private Gauge _nodeDiskWearout = null!;

    private void InitNodeDiskMetrics(MetricFactory mf)
    {
        var labels = new GaugeConfiguration { LabelNames = ["node", "serial", "type", "dev_path"] };

        _nodeDiskHealth = mf.CreateGauge("cv4pve_node_disk_health",
                                         "Disk health from SMART (1 = PASSED, 0 = otherwise)",
                                         labels);

        _nodeDiskWearout = mf.CreateGauge("cv4pve_node_disk_wearout",
                                          "Disk wearout indicator from SMART (percentage)",
                                          labels);
    }

    private void WriteNodeDiskMetrics(ClusterStatus node, IEnumerable<NodeDiskList> disks)
    {
        foreach (var disk in disks)
        {
            var labels = new[] { node.Name, disk.Serial ?? "", disk.Type ?? "", disk.DevPath ?? "" };

            _nodeDiskHealth.WithLabels(labels).Set(ToBit(disk.Health == "PASSED"));

            if (!string.IsNullOrWhiteSpace(disk.Wearout) && disk.Wearout != "N/A"
                && double.TryParse(disk.Wearout, NumberStyles.Float, CultureInfo.InvariantCulture, out var wearout))
            {
                _nodeDiskWearout.WithLabels(labels).Set(wearout);
            }
        }
    }
}
