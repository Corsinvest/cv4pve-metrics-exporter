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
    private Gauge _nodeVersionInfo = null!;

    private void InitNodeVersionMetrics(MetricFactory mf)
        => _nodeVersionInfo = mf.CreateGauge("cv4pve_node_version_info",
                                             "Node Proxmox VE version info (always 1)",
                                             new GaugeConfiguration { LabelNames = ["node", "version", "release", "repoid"] });

    private void WriteNodeVersionMetrics(ClusterStatus node, NodeVersion version)
        => _nodeVersionInfo.WithLabels(node.Name,
                                       version.Version ?? "",
                                       version.Release ?? "",
                                       version.RepositoryId ?? "")
                           .Set(1);
}
