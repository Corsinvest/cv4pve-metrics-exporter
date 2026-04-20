/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

/// <summary>Settings for the Prometheus metrics engine.</summary>
public class Settings
{
    /// <summary>Enable this exporter.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>HTTP listener host.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>HTTP listener port.</summary>
    public int Port { get; set; } = 9221;

    /// <summary>HTTP listener URL path.</summary>
    public string Url { get; set; } = "metrics/";

    /// <summary>Max parallel requests when fetching per-node/per-guest data.</summary>
    public int MaxParallelRequests { get; set; } = 5;

    /// <summary>Instrument Proxmox API calls (duration histogram + errors counter per endpoint).</summary>
    public bool ApiInstrumentation { get; set; } = true;

    /// <summary>Cluster-wide collection toggles.</summary>
    public ClusterSettings Cluster { get; set; } = new();

    /// <summary>Node-level collection toggles.</summary>
    public NodeSettings Node { get; set; } = new();

    /// <summary>Guest (VM/CT) collection toggles.</summary>
    public GuestSettings Guest { get; set; } = new();

    /// <summary>Minimum API calls — only cluster-wide bulk data.</summary>
    public static Settings Fast() => new()
    {
        ApiInstrumentation = false,
        Node = new()
        {
            Status = new() { Enabled = false },
            Subscription = new() { Enabled = false },
            Replication = new() { Enabled = false },
            DiskSmart = new() { Enabled = false },
        },
    };

    /// <summary>Default — everything cheap is on, slow-changing data is cached, expensive opt-ins are off.</summary>
    public static Settings Standard() => new()
    {
        Cluster = new()
        {
            BackupInfo = new() { Enabled = true, CacheSeconds = 600 },
        },
        Node = new()
        {
            Subscription = new() { Enabled = true, CacheSeconds = 3600 },
        },
    };

    /// <summary>Everything enabled, with cache TTL tuned per data freshness needs.</summary>
    public static Settings Full() => new()
    {
        Cluster = new()
        {
            Ha = new() { Enabled = true, CacheSeconds = 30 },
            BackupInfo = new() { Enabled = true, CacheSeconds = 600 },
        },
        Node = new()
        {
            Status = new() { Enabled = true, CacheSeconds = 0 },
            Subscription = new() { Enabled = true, CacheSeconds = 3600 },
            Replication = new() { Enabled = true, CacheSeconds = 60 },
            DiskSmart = new() { Enabled = true, CacheSeconds = 600 },
        },
        Guest = new()
        {
            Balloon = new() { Enabled = true, CacheSeconds = 0 },
        },
    };
}
