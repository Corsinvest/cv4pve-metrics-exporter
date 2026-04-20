/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using PrometheusSettings = Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus.Settings;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api;

/// <summary>Top-level settings container. Holds the configuration for every available exporter.</summary>
public class Settings
{
    /// <summary>Prometheus exporter settings.</summary>
    public PrometheusSettings Prometheus { get; set; } = new();

    /// <summary>Fast profile — minimum API calls, only cluster-wide bulk data.</summary>
    public static Settings Fast() => new() { Prometheus = PrometheusSettings.Fast() };

    /// <summary>Standard profile (default) — everything cheap is on, expensive opt-ins are off.</summary>
    public static Settings Standard() => new();

    /// <summary>Full profile — everything enabled, including SMART and balloon.</summary>
    public static Settings Full() => new() { Prometheus = PrometheusSettings.Full() };
}
