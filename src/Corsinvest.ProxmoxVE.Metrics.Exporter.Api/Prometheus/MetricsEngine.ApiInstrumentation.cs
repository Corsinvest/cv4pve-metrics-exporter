/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Api;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    [GeneratedRegex(@"/nodes/[^/]+")]
    private static partial Regex NodesRegex();

    [GeneratedRegex(@"/(qemu|lxc)/\d+")]
    private static partial Regex GuestIdRegex();

    [GeneratedRegex(@"/tasks/[^/]+")]
    private static partial Regex TasksUpidRegex();

    private Histogram _apiRequestDuration = null!;
    private Counter _apiRequestErrors = null!;

    private void InitApiInstrumentationMetrics(MetricFactory mf)
    {
        _apiRequestDuration = mf.CreateHistogram("cv4pve_api_request_duration_seconds",
                                                 "Duration of Proxmox API requests",
                                                 new HistogramConfiguration
                                                 {
                                                     LabelNames = ["method", "endpoint"],
                                                     Buckets = [0.05, 0.1, 0.25, 0.5, 1, 2.5, 5, 10, 30],
                                                 });

        _apiRequestErrors = mf.CreateCounter("cv4pve_api_request_errors_total",
                                             "Total number of failed Proxmox API requests",
                                             new CounterConfiguration { LabelNames = ["method", "endpoint"] });
    }

    private void OnApiRequestCompleted(object? _, Result result)
    {
        var method = result.MethodType.ToString();
        var endpoint = NormalizeEndpoint(result.RequestResource);

        _apiRequestDuration.WithLabels(method, endpoint).Observe(result.Duration.TotalSeconds);
        if (!result.IsSuccessStatusCode) { _apiRequestErrors.WithLabels(method, endpoint).Inc(); }
    }

    private static string NormalizeEndpoint(string resource)
    {
        var s = resource;
        s = NodesRegex().Replace(s, "/nodes/{node}");
        s = GuestIdRegex().Replace(s, "/$1/{vmid}");
        s = TasksUpidRegex().Replace(s, "/tasks/{upid}");
        return s;
    }
}
