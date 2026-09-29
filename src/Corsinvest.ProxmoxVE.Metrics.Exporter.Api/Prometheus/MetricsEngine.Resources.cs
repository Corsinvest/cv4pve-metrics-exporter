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
        var series = new Series();

        foreach (var item in _resources.Where(r => !string.IsNullOrEmpty(r.Id)))
        {
            switch (item.ResourceType)
            {
                case ClusterResourceType.Vm:
                    series.Set(_guestInfo,
                               1,
                               item.Id,
                               item.VmId.ToString(),
                               item.Node ?? "",
                               item.Name ?? "",
                               item.VmType.ToString().ToLowerInvariant(),
                               SortedCsv(item.Tags, ';'),
                               ToBit(item.IsTemplate).ToString());

                    WriteGuestLock(series, item);

                    series.Set(_guestCpuUsage, item.CpuUsagePercentage, item.Id);
                    series.Set(_guestCpuCores, item.CpuSize, item.Id);
                    series.Set(_guestMemorySize, item.MemorySize, item.Id);
                    series.Set(_guestMemoryUsage, item.MemoryUsage, item.Id);
                    series.Set(_guestMemoryHostRatio, item.HostMemoryUsage, item.Id);
                    series.Set(_guestDiskSize, item.DiskSize, item.Id);
                    series.Set(_guestDiskUsage, item.DiskUsage, item.Id);
                    series.Set(_guestUptime, item.Uptime, item.Id);

                    series.SetCounter(_guestDiskRead, item.DiskRead, item.Id);
                    series.SetCounter(_guestDiskWrite, item.DiskWrite, item.Id);
                    series.SetCounter(_guestNetIn, item.NetIn, item.Id);
                    series.SetCounter(_guestNetOut, item.NetOut, item.Id);
                    break;

                case ClusterResourceType.Storage:
                    series.Set(_storageInfo, 1, item.Id, item.Node ?? "", item.Storage ?? "", SortedCsv(item.Content, ','));
                    series.Set(_storageShared, ToBit(item.Shared), item.Id);
                    series.Set(_storageSize, item.DiskSize, item.Id);
                    series.Set(_storageUsage, item.DiskUsage, item.Id);
                    break;
            }
        }

        foreach (var gauge in new[] { _guestInfo, _guestLock, _guestCpuUsage, _guestCpuCores, _guestMemorySize, _guestMemoryUsage,
                                      _guestMemoryHostRatio, _guestDiskSize, _guestDiskUsage, _guestUptime,
                                      _storageInfo, _storageShared, _storageSize, _storageUsage })
        {
            series.Prune(gauge);
        }

        foreach (var counter in new[] { _guestDiskRead, _guestDiskWrite, _guestNetIn, _guestNetOut }) { series.Prune(counter); }

        WriteResourceStatusMetrics(series);
    }

    private static string SortedCsv(string? csv, char separator)
    {
        if (string.IsNullOrWhiteSpace(csv)) { return ""; }
        var parts = csv.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Array.Sort(parts, StringComparer.Ordinal);
        return string.Join(separator, parts);
    }
}
