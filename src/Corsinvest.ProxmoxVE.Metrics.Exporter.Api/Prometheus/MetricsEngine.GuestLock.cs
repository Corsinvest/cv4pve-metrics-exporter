/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private static readonly string[] LockStates =
    [
        "backup",
        "clone",
        "create",
        "migrate",
        "rollback",
        "snapshot",
        "snapshot-delete",
        "suspended",
        "suspending",
    ];

    private Gauge _guestLock = null!;

    private void InitGuestLockMetrics(MetricFactory mf)
        => _guestLock = mf.CreateGauge("cv4pve_guest_lock",
                                       "Guest lock state (1 if matches state, 0 otherwise)",
                                       new GaugeConfiguration { LabelNames = ["id", "state"] });

    private void WriteGuestLock(ClusterResource item)
    {
        var current = item.Lock ?? "";
        foreach (var state in LockStates)
        {
            _guestLock.WithLabels(item.Id, state)
                      .Set(ToBit(string.Equals(current, state, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
