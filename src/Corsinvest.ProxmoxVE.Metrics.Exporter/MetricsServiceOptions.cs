/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter;

internal sealed class MetricsServiceOptions
{
    public Func<Task<PveClient>> ClientFactory { get; set; } = null!;
    public Settings Settings { get; set; } = new();

    /// <summary>Exit code of the process: 1 when the exporter could not start.</summary>
    public int ExitCode { get; set; }
}
