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
    private Gauge _nodeUptime = null!;
    private Gauge _nodeLoadAvg1 = null!;
    private Gauge _nodeLoadAvg5 = null!;
    private Gauge _nodeLoadAvg15 = null!;
    private Gauge _nodeMemoryUsed = null!;
    private Gauge _nodeMemoryTotal = null!;
    private Gauge _nodeMemoryAssigned = null!;
    private Gauge _nodeSwapUsed = null!;
    private Gauge _nodeSwapTotal = null!;
    private Gauge _nodeRootFsUsed = null!;
    private Gauge _nodeRootFsTotal = null!;
    private Gauge _nodeCpuAssigned = null!;

    private void InitNodeStatusMetrics(MetricFactory mf)
    {
        var nodeLabel = new GaugeConfiguration { LabelNames = ["node"] };

        _nodeUptime = mf.CreateGauge("cv4pve_node_uptime_seconds", "Node uptime in seconds", nodeLabel);
        _nodeLoadAvg1 = mf.CreateGauge("cv4pve_node_load_avg1", "Node load avg 1 min", nodeLabel);
        _nodeLoadAvg5 = mf.CreateGauge("cv4pve_node_load_avg5", "Node load avg 5 min", nodeLabel);
        _nodeLoadAvg15 = mf.CreateGauge("cv4pve_node_load_avg15", "Node load avg 15 min", nodeLabel);

        _nodeMemoryUsed = mf.CreateGauge("cv4pve_node_memory_used_bytes", "Node memory used", nodeLabel);
        _nodeMemoryTotal = mf.CreateGauge("cv4pve_node_memory_total_bytes", "Node memory total", nodeLabel);
        _nodeMemoryAssigned = mf.CreateGauge("cv4pve_node_memory_assigned_bytes", "Sum of configured memory of running guests on this node", nodeLabel);

        _nodeSwapUsed = mf.CreateGauge("cv4pve_node_swap_used_bytes", "Node swap used", nodeLabel);
        _nodeSwapTotal = mf.CreateGauge("cv4pve_node_swap_total_bytes", "Node swap total", nodeLabel);

        _nodeRootFsUsed = mf.CreateGauge("cv4pve_node_root_fs_used_bytes", "Node root fs used", nodeLabel);
        _nodeRootFsTotal = mf.CreateGauge("cv4pve_node_root_fs_total_bytes", "Node root fs total", nodeLabel);

        _nodeCpuAssigned = mf.CreateGauge("cv4pve_node_cpu_assigned_cores", "Sum of CPU cores allocated to running guests on this node", nodeLabel);
    }

    private void WriteNodeStatusMetrics(ClusterStatus node, NodeStatus st)
    {
        _nodeUptime.WithLabels(node.Name).Set(st.Uptime);

        if (st.Memory is { } mem)
        {
            _nodeMemoryUsed.WithLabels(node.Name).Set(mem.Used);
            _nodeMemoryTotal.WithLabels(node.Name).Set(mem.Total);
        }

        if (st.Swap is { } swap)
        {
            _nodeSwapUsed.WithLabels(node.Name).Set(swap.Used);
            _nodeSwapTotal.WithLabels(node.Name).Set(swap.Total);
        }

        if (st.RootFs is { } root)
        {
            _nodeRootFsUsed.WithLabels(node.Name).Set(root.Used);
            _nodeRootFsTotal.WithLabels(node.Name).Set(root.Total);
        }

        var loadAvg = st.LoadAvg?.ToArray();
        if (loadAvg is { Length: >= 3 })
        {
            _nodeLoadAvg1.WithLabels(node.Name).Set(double.Parse(loadAvg[0], CultureInfo.InvariantCulture));
            _nodeLoadAvg5.WithLabels(node.Name).Set(double.Parse(loadAvg[1], CultureInfo.InvariantCulture));
            _nodeLoadAvg15.WithLabels(node.Name).Set(double.Parse(loadAvg[2], CultureInfo.InvariantCulture));
        }
    }

    private void WriteNodeAssignmentMetrics()
    {
        foreach (var node in _resources.Where(r => r.ResourceType == ClusterResourceType.Node))
        {
            _nodeCpuAssigned.WithLabels(node.Node).Set(node.NodeCpuAssigned);
            _nodeMemoryAssigned.WithLabels(node.Node).Set(node.NodeMemoryAssigned);
        }
    }
}
