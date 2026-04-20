/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

/// <summary>Per-node collection toggles. Each enabled collector adds 1 API call per node per scrape (unless cached).</summary>
public class NodeSettings
{
    /// <summary>Memory/swap/load/uptime + node version.</summary>
    public CollectorSettings Status { get; set; } = new();

    /// <summary>Subscription status and level.</summary>
    public CollectorSettings Subscription { get; set; } = new();

    /// <summary>Replication jobs status.</summary>
    public CollectorSettings Replication { get; set; } = new();

    /// <summary>Disk SMART/wearout.</summary>
    public CollectorSettings DiskSmart { get; set; } = new() { Enabled = false };
}
