/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Globalization;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
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

    private Task CollectBalloonAsync(PveClient client)
        => RunParallelAsync(_resources.Where(r => r.ResourceType == ClusterResourceType.Vm
                                                    && r.IsRunning
                                                    && r.VmType == VmType.Qemu),
                             vm => FetchBalloonForGuestAsync(client, vm));

    private async Task FetchBalloonForGuestAsync(PveClient client, ClusterResource vm)
    {
        var response = await client.Nodes[vm.Node].Qemu[vm.VmId].Monitor.Monitor("info balloon");
        if (response?.Response?.data is not string data || !data.StartsWith(KeyBalloon)) { return; }

        var payload = data[KeyBalloon.Length..];
        foreach (var token in payload.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = token.Split('=');
            if (kv.Length == 2
                && kv[0] == "actual"
                && double.TryParse(kv[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                _guestBalloonActual.WithLabels(vm.Id, vm.VmId.ToString()).Set(v * 1024 * 1024);
                return;
            }
        }
    }
}
