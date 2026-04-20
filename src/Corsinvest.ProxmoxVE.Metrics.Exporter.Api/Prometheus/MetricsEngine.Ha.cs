/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private static readonly string[] HaGuestStates =
    [
        "stopped",
        "request_stop",
        "request_start",
        "request_start_balance",
        "started",
        "fence",
        "recovery",
        "migrate",
        "relocate",
        "freeze",
        "error",
        "disabled",
    ];

    private static readonly string[] HaNodeStates =
    [
        "online",
        "maintenance",
        "unknown",
        "fence",
        "gone",
    ];

    private Gauge _haState = null!;
    private Gauge _haNodeState = null!;
    private Gauge _haQuorate = null!;

    private void InitHaMetrics(MetricFactory mf)
    {
        _haState = mf.CreateGauge("cv4pve_ha_state",
                                  "HA service state (1 if matches state, 0 otherwise)",
                                  new GaugeConfiguration { LabelNames = ["sid", "type", "group", "state"] });

        _haNodeState = mf.CreateGauge("cv4pve_ha_node_state",
                                      "HA node state (1 if matches state, 0 otherwise)",
                                      new GaugeConfiguration { LabelNames = ["node", "state"] });

        _haQuorate = mf.CreateGauge("cv4pve_ha_quorate",
                                    "1 if the cluster HA manager reports quorum, 0 otherwise",
                                    new GaugeConfiguration { LabelNames = [] });
    }

    private void WriteHaMetrics(IEnumerable<ClusterHaResource> resources)
    {
        foreach (var ha in resources.Where(r => r.Type is "vm" or "ct" && !string.IsNullOrEmpty(r.Sid)))
        {
            foreach (var state in HaGuestStates)
            {
                _haState.WithLabels(ha.Sid,
                                    ha.Type,
                                    ha.Group ?? "",
                                    state)
                        .Set(ToBit(string.Equals(ha.State ?? "", state, StringComparison.OrdinalIgnoreCase)));
            }
        }
    }

    private void WriteHaStatusMetrics(IEnumerable<ClusterHaStatusCurrent> status)
    {
        foreach (var entry in status)
        {
            switch (entry.Type)
            {
                case "node":
                    foreach (var state in HaNodeStates)
                    {
                        _haNodeState.WithLabels(entry.Node ?? "", state)
                                    .Set(ToBit(string.Equals(entry.Status ?? "", state, StringComparison.OrdinalIgnoreCase)));
                    }
                    break;

                case "quorum":
                    _haQuorate.WithLabels().Set(ToBit(entry.Quorate));
                    break;
            }
        }
    }
}
