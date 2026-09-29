/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

/// <summary>Cluster-wide collection toggles.</summary>
public class ClusterSettings
{
    /// <summary>HA resources and status.</summary>
    public CollectorSettings Ha { get; set; } = new();

    /// <summary>Guests not covered by any backup job. Cached 10 minutes by default: it changes only when jobs or guests do.</summary>
    public CollectorSettings BackupInfo { get; set; } = new() { CacheSeconds = 600 };
}
