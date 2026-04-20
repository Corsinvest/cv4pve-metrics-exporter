/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

/// <summary>Per-guest collection toggles.</summary>
public class GuestSettings
{
    /// <summary>QEMU balloon memory via monitor RPC (1 call per running QEMU).</summary>
    public CollectorSettings Balloon { get; set; } = new() { Enabled = false };
}
