/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Collections.Concurrent;
using System.Globalization;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Extensions;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private const string KeyBalloon = "balloon: ";

    private Gauge _guestBalloonActual = null!;

    private void InitBalloonMetrics(MetricFactory mf)
        => _guestBalloonActual = mf.CreateGauge("cv4pve_guest_balloon_actual_bytes",
                                                "Guest QEMU balloon actual memory in bytes",
                                                new GaugeConfiguration { LabelNames = ["id", "vmid"] });

    private async Task CollectBalloonAsync(PveClient client)
    {
        var vms = _resources.Where(r => r.ResourceType == ClusterResourceType.Vm
                                        && r.IsRunning
                                        && r.VmType == VmType.Qemu)
                            .ToArray();

        var calls = new ConcurrentDictionary<string, Task<Result>>();
        await RunParallelAsync(vms, async vm =>
        {
            var call = Checked(Fork(client).Nodes[vm.Node].Qemu[vm.VmId].Monitor.Monitor("info balloon"));
            calls[vm.Id] = call;
            await SafeTaskExtensions.WhenAllSafe(call);
        });

        TrackErrors("guest", [.. calls.Values]);

        var series = new Series();
        foreach (var vm in vms)
        {
            if (calls.TryGetValue(vm.Id, out var call)
                && call.IsCompletedSuccessfully
                && ParseBalloonActual(call.Result) is { } actual)
            {
                series.Set(_guestBalloonActual, actual, vm.Id, vm.VmId.ToString());
            }
        }

        // A VM whose call failed keeps its last value; stopped, deleted or balloon-less VMs are removed.
        var failed = calls.Where(a => !a.Value.IsCompletedSuccessfully).Select(a => a.Key).ToHashSet(StringComparer.Ordinal);
        series.Prune(_guestBalloonActual, labels => !failed.Contains(labels[0]));

        if (failed.Count == 0) { MarkCollected("guest:balloon", _settings.Guest.Balloon); }
    }

    /// <summary>Actual balloon size in bytes from the <c>info balloon</c> answer ("balloon: actual=2048 …", in MiB).</summary>
    private static double? ParseBalloonActual(Result result)
    {
        if (result.Response?.data is not string data || !data.StartsWith(KeyBalloon)) { return null; }

        foreach (var token in data[KeyBalloon.Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = token.Split('=');
            if (kv.Length == 2
                && kv[0] == "actual"
                && double.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                return v * 1024 * 1024;
            }
        }

        return null;
    }
}
