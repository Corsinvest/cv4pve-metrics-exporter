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

    // smartctl reports "PASSED" for ATA and NVMe disks, "OK" for SAS disks.
    private static bool IsHealthy(string? health) => health is "PASSED" or "OK";

    private void WriteNodeDiskMetrics(ClusterStatus node, IEnumerable<NodeDiskList> disks)
    {
        var series = new Series();
        foreach (var disk in disks)
        {
            var labels = new[] { node.Name, disk.Serial ?? "", disk.Type ?? "", disk.DevPath ?? "" };

            series.Set(_nodeDiskHealth, ToBit(IsHealthy(disk.Health)), labels);

            if (!string.IsNullOrWhiteSpace(disk.Wearout) && disk.Wearout != "N/A"
                && double.TryParse(disk.Wearout, NumberStyles.Float, CultureInfo.InvariantCulture, out var wearout))
            {
                series.Set(_nodeDiskWearout, wearout, labels);
            }
        }

        series.Prune(_nodeDiskHealth, labels => labels[0] == node.Name);
        series.Prune(_nodeDiskWearout, labels => labels[0] == node.Name);
    }

    private void RemoveNodeDiskSeries(Func<string[], bool> predicate)
    {
        RemoveWhere(_nodeDiskHealth, predicate);
        RemoveWhere(_nodeDiskWearout, predicate);
    }
}
