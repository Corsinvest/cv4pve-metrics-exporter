/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Newtonsoft.Json.Linq;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    // Service states of the HA manager (CRM), pve-ha-manager src/PVE/HA/Manager.pm $valid_service_states.
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
    ];

    // Node states of the HA manager, pve-ha-manager src/PVE/HA/NodeStatus.pm $valid_node_states.
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

    /// <summary>
    /// Resources from /cluster/ha/resources (sid, type, group); their state, the node states and the quorum
    /// from /cluster/ha/status/manager_status.
    /// </summary>
    private void WriteHaMetrics(IEnumerable<ClusterHaResource> resources, Result managerStatus)
    {
        var data = managerStatus.Response?.data is object d ? JToken.FromObject(d) : null;
        var status = data?["manager_status"];
        var services = status?["service_status"] as JObject;
        var series = new Series();

        foreach (var ha in resources.Where(r => r.Type is "vm" or "ct" && !string.IsNullOrEmpty(r.Sid)))
        {
            var current = (string?)services?[ha.Sid]?["state"] ?? "";
            foreach (var state in HaGuestStates)
            {
                series.Set(_haState, ToBit(current == state), ha.Sid, ha.Type, ha.Group ?? "", state);
            }
        }

        if (status?["node_status"] is JObject nodes)
        {
            foreach (var (node, value) in nodes)
            {
                var current = (string?)value ?? "";
                foreach (var state in HaNodeStates)
                {
                    series.Set(_haNodeState, ToBit(current == state), node, state);
                }
            }
        }

        if (data?["quorum"]?["quorate"] is JValue quorate)
        {
            _haQuorate.WithLabels().Set(ToBit(quorate.Type == JTokenType.Boolean ? (bool)quorate : (long?)quorate > 0));
        }

        series.Prune(_haState);
        series.Prune(_haNodeState);
    }
}
