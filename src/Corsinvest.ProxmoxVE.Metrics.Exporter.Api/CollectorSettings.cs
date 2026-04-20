/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api;

/// <summary>Common settings for a metrics collector.</summary>
public class CollectorSettings
{
    /// <summary>Enable this collector.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Cache TTL in seconds. 0 = no cache (collect every scrape).</summary>
    public int CacheSeconds { get; set; } = 0;
}
