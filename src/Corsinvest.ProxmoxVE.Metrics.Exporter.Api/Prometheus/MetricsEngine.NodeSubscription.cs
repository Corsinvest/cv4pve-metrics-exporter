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
        var series = new Series();
        series.Set(_nodeSubscriptionInfo, 1, node.Name, NodeHelper.DecodeLevelSupport(sub.Level).ToString());

        foreach (var s in SubscriptionStatuses)
        {
            series.Set(_nodeSubscriptionStatus, ToBit(string.Equals(sub.Status, s, StringComparison.OrdinalIgnoreCase)), node.Name, s);
        }

        if (!string.IsNullOrWhiteSpace(sub.NextDuedate)
            && DateTime.TryParse(sub.NextDuedate,
                                 CultureInfo.InvariantCulture,
                                 DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                 out var due))
        {
            series.Set(_nodeSubscriptionNextDue, new DateTimeOffset(due, TimeSpan.Zero).ToUnixTimeSeconds(), node.Name);
        }

        bool OfNode(string[] labels) => labels[0] == node.Name;
        series.Prune(_nodeSubscriptionInfo, OfNode);
        series.Prune(_nodeSubscriptionStatus, OfNode);
        series.Prune(_nodeSubscriptionNextDue, OfNode);
    }

    private void RemoveNodeSubscriptionSeries(Func<string[], bool> predicate)
    {
        RemoveWhere(_nodeSubscriptionInfo, predicate);
        RemoveWhere(_nodeSubscriptionStatus, predicate);
        RemoveWhere(_nodeSubscriptionNextDue, predicate);
    }
}
