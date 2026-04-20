/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private Gauge _guestInfo = null!;
    private Gauge _guestCpuUsage = null!;
    private Gauge _guestCpuCores = null!;
    private Gauge _guestMemorySize = null!;
    private Gauge _guestMemoryUsage = null!;
    private Gauge _guestMemoryHostRatio = null!;
    private Gauge _guestDiskSize = null!;
    private Gauge _guestDiskUsage = null!;
    private Gauge _guestUptime = null!;
    private Counter _guestDiskRead = null!;
    private Counter _guestDiskWrite = null!;
    private Counter _guestNetIn = null!;
    private Counter _guestNetOut = null!;

    private Gauge _storageInfo = null!;
    private Gauge _storageShared = null!;
    private Gauge _storageSize = null!;
    private Gauge _storageUsage = null!;

    private void InitResourceMetrics(MetricFactory mf)
    {
        var idLabel = new GaugeConfiguration { LabelNames = ["id"] };
        var idLabelCounter = new CounterConfiguration { LabelNames = ["id"] };

        _guestInfo = mf.CreateGauge("cv4pve_guest_info",
                                    "VM/CT info (always 1)",
                                    new GaugeConfiguration
                                    {
                                        LabelNames = ["id", "vmid", "node", "name", "type", "tags", "template"]
                                    });

        _guestCpuUsage = mf.CreateGauge("cv4pve_guest_cpu_usage_ratio", "Guest CPU usage ratio (0..1)", idLabel);
        _guestCpuCores = mf.CreateGauge("cv4pve_guest_cpu_cores", "Guest CPU cores allocated", idLabel);
        _guestMemorySize = mf.CreateGauge("cv4pve_guest_memory_size_bytes", "Guest configured memory in bytes", idLabel);
        _guestMemoryUsage = mf.CreateGauge("cv4pve_guest_memory_usage_bytes", "Guest memory usage in bytes", idLabel);
        _guestMemoryHostRatio = mf.CreateGauge("cv4pve_guest_memory_host_ratio", "Guest memory usage over host total (0..1)", idLabel);
        _guestDiskSize = mf.CreateGauge("cv4pve_guest_disk_size_bytes", "Guest disk size in bytes", idLabel);
        _guestDiskUsage = mf.CreateGauge("cv4pve_guest_disk_usage_bytes", "Guest disk used bytes", idLabel);
        _guestUptime = mf.CreateGauge("cv4pve_guest_uptime_seconds", "Guest uptime in seconds", idLabel);

        _guestDiskRead = mf.CreateCounter("cv4pve_guest_disk_read_bytes_total", "Total bytes read from storage", idLabelCounter);
        _guestDiskWrite = mf.CreateCounter("cv4pve_guest_disk_write_bytes_total", "Total bytes written to storage", idLabelCounter);
        _guestNetIn = mf.CreateCounter("cv4pve_guest_network_receive_bytes_total", "Total bytes received over network", idLabelCounter);
        _guestNetOut = mf.CreateCounter("cv4pve_guest_network_transmit_bytes_total", "Total bytes transmitted over network", idLabelCounter);

        _storageInfo = mf.CreateGauge("cv4pve_storage_info",
                                      "Storage info (always 1)",
                                      new GaugeConfiguration { LabelNames = ["id", "node", "storage", "content"] });

        _storageShared = mf.CreateGauge("cv4pve_storage_shared", "1 if the storage is shared across nodes, 0 otherwise", idLabel);
        _storageSize = mf.CreateGauge("cv4pve_storage_size_bytes", "Storage total size in bytes", idLabel);
        _storageUsage = mf.CreateGauge("cv4pve_storage_usage_bytes", "Storage used bytes", idLabel);
    }

    private void WriteResourceMetrics()
    {
        foreach (var item in _resources.Where(r => !string.IsNullOrEmpty(r.Id)))
        {
            switch (item.ResourceType)
            {
                case ClusterResourceType.Vm:
                    _guestInfo.WithLabels(item.Id,
                                          item.VmId.ToString(),
                                          item.Node ?? "",
                                          item.Name ?? "",
                                          item.VmType.ToString().ToLowerInvariant(),
                                          SortedCsv(item.Tags, ';'),
                                          ToBit(item.IsTemplate).ToString())
                              .Set(1);

                    WriteGuestLock(item);

                    _guestCpuUsage.WithLabels(item.Id).Set(item.CpuUsagePercentage);
                    _guestCpuCores.WithLabels(item.Id).Set(item.CpuSize);
                    _guestMemorySize.WithLabels(item.Id).Set(item.MemorySize);
                    _guestMemoryUsage.WithLabels(item.Id).Set(item.MemoryUsage);
                    _guestMemoryHostRatio.WithLabels(item.Id).Set(item.HostMemoryUsage);
                    _guestDiskSize.WithLabels(item.Id).Set(item.DiskSize);
                    _guestDiskUsage.WithLabels(item.Id).Set(item.DiskUsage);
                    _guestUptime.WithLabels(item.Id).Set(item.Uptime);

                    SetCounter(_guestDiskRead, item.Id, item.DiskRead);
                    SetCounter(_guestDiskWrite, item.Id, item.DiskWrite);
                    SetCounter(_guestNetIn, item.Id, item.NetIn);
                    SetCounter(_guestNetOut, item.Id, item.NetOut);
                    break;

                case ClusterResourceType.Storage:
                    _storageInfo.WithLabels(item.Id, item.Node ?? "", item.Storage ?? "", SortedCsv(item.Content, ',')).Set(1);
                    _storageShared.WithLabels(item.Id).Set(ToBit(item.Shared));
                    _storageSize.WithLabels(item.Id).Set(item.DiskSize);
                    _storageUsage.WithLabels(item.Id).Set(item.DiskUsage);
                    break;
            }
        }
    }

    private static string SortedCsv(string? csv, char separator)
    {
        if (string.IsNullOrWhiteSpace(csv)) { return ""; }
        var parts = csv.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Array.Sort(parts, StringComparer.Ordinal);
        return string.Join(separator, parts);
    }

    private static void SetCounter(Counter counter, string id, double value)
    {
        var current = counter.WithLabels(id).Value;
        if (value > current) { counter.WithLabels(id).Inc(value - current); }
    }
}
