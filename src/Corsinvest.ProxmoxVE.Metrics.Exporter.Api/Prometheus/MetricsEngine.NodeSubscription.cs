/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Globalization;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private static readonly string[] SubscriptionStatuses =
    [
        "active", "expired", "new", "notfound", "invalid", "suspended",
    ];

    private Gauge _nodeSubscriptionInfo = null!;
    private Gauge _nodeSubscriptionStatus = null!;
    private Gauge _nodeSubscriptionNextDue = null!;

    private void InitNodeSubscriptionMetrics(MetricFactory mf)
    {
        _nodeSubscriptionInfo = mf.CreateGauge("cv4pve_node_subscription_info",
                                               "Node subscription info (always 1)",
                                               new GaugeConfiguration { LabelNames = ["node", "level"] });

        _nodeSubscriptionStatus = mf.CreateGauge("cv4pve_node_subscription_status",
                                                 "Node subscription state (1 if matches status, 0 otherwise)",
                                                 new GaugeConfiguration { LabelNames = ["node", "status"] });

        _nodeSubscriptionNextDue = mf.CreateGauge("cv4pve_node_subscription_next_due_timestamp_seconds",
                                                  "Node subscription next due date as Unix timestamp",
                                                  new GaugeConfiguration { LabelNames = ["node"] });
    }

    private void WriteNodeSubscriptionMetrics(ClusterStatus node, NodeSubscription sub)
    {
        _nodeSubscriptionInfo.WithLabels(node.Name,
                                         NodeHelper.DecodeLevelSupport(sub.Level).ToString())
                             .Set(1);

        foreach (var s in SubscriptionStatuses)
        {
            _nodeSubscriptionStatus.WithLabels(node.Name, s)
                .Set(ToBit(string.Equals(sub.Status, s, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(sub.NextDuedate)
            && DateTime.TryParse(sub.NextDuedate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var due))
        {
            _nodeSubscriptionNextDue.WithLabels(node.Name).Set(new DateTimeOffset(due).ToUnixTimeSeconds());
        }
    }
}
