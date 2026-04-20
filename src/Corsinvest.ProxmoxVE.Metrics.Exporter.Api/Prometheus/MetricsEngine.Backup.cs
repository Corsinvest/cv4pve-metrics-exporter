/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private Gauge _guestsNotBackedUp = null!;
    private Gauge _notBackedUpInfo = null!;

    private void InitBackupMetrics(MetricFactory mf)
    {
        _guestsNotBackedUp = mf.CreateGauge("cv4pve_guests_not_backed_up",
                                            "Number of guests not covered by any backup job",
                                            new GaugeConfiguration { LabelNames = [] });

        _notBackedUpInfo = mf.CreateGauge("cv4pve_not_backed_up_info",
                                          "1 if the guest is not covered by any backup job",
                                          new GaugeConfiguration { LabelNames = ["id"] });
    }

    private void WriteBackupMetrics(Result result)
    {
        if (result.Response?.data is not IEnumerable<dynamic> entries) { return; }

        var count = 0;
        foreach (var entry in entries)
        {
            var type = (string?)entry.type ?? "";
            var vmid = (string?)(entry.vmid?.ToString()) ?? "";
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(vmid)) { continue; }

            _notBackedUpInfo.WithLabels($"{type}/{vmid}").Set(1);
            count++;
        }

        _guestsNotBackedUp.WithLabels().Set(count);
    }
}
