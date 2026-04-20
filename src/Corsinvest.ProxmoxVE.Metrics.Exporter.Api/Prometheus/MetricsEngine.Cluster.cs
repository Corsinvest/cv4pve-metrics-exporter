/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private Gauge _clusterInfo = null!;
    private Gauge _clusterQuorate = null!;
    private Gauge _clusterNodes = null!;
    private Gauge _nodeInfo = null!;

    private void InitClusterMetrics(MetricFactory mf)
    {
        _clusterInfo = mf.CreateGauge("cv4pve_cluster_info",
                                      "Cluster info (always 1)",
                                      new GaugeConfiguration { LabelNames = ["name", "version"] });

        _clusterQuorate = mf.CreateGauge("cv4pve_cluster_quorate",
                                         "1 if the cluster is quorate, 0 otherwise",
                                         new GaugeConfiguration { LabelNames = ["name"] });

        _clusterNodes = mf.CreateGauge("cv4pve_cluster_nodes",
                                       "Number of nodes in the cluster",
                                       new GaugeConfiguration { LabelNames = ["name"] });

        _nodeInfo = mf.CreateGauge("cv4pve_node_info",
                                   "Node info (always 1)",
                                   new GaugeConfiguration { LabelNames = ["id", "name", "ip", "level"] });
    }

    private void WriteClusterMetrics()
    {
        foreach (var item in _statusEntries)
        {
            if (item.Type == "cluster")
            {
                var name = item.Name ?? "";
                _clusterInfo.WithLabels(name, item.Version.ToString()).Set(1);
                _clusterQuorate.WithLabels(name).Set(item.Quorate);
                _clusterNodes.WithLabels(name).Set(item.Nodes);
            }
            else if (item.Type == "node" && !string.IsNullOrEmpty(item.Id))
            {
                _nodeInfo.WithLabels(item.Id,
                                     item.Name ?? "",
                                     item.IpAddress ?? "",
                                     NodeHelper.DecodeLevelSupport(item.Level).ToString())
                         .Set(1);
            }
        }
    }
}
