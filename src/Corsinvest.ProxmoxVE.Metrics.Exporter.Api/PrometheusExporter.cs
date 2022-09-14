/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api
{
    /// <summary>
    /// Prometheus Exporter
    /// </summary>
    public class PrometheusExporter
    {
        private const string KeyBalloon = "balloon: ";

        /// <summary>
        /// Default host
        /// </summary>
        public static readonly string DEFAULT_HOST = "localhost";

        /// <summary>
        /// Default port
        /// </summary>
        public static readonly int DEFAULT_PORT = 9221;

        /// <summary>
        /// Default url
        /// </summary>
        public static readonly string DEFAULT_URL = "metrics/";

        /// <summary>
        /// Default prefix
        /// </summary>
        public static readonly string DEFAULT_PREFIX = "cv4pve";

        private readonly Gauge _up;
        private readonly Gauge _nodeInfo;
        private readonly Gauge _nodeDiskWearout;
        private readonly Gauge _nodeDiskHealth;
        private readonly Gauge _nodeDiskSmart;
        private readonly Gauge _clusterInfo;
        private readonly Gauge _versionInfo;
        private readonly Gauge _guestInfo;
        private readonly Gauge _storageInfo;
        //private readonly Gauge StorageDef;
        private readonly Dictionary<string, Gauge> _resourceInfo = new Dictionary<string, Gauge>();
        private readonly Dictionary<string, Gauge> _guestBalloonInfo = new Dictionary<string, Gauge>();
        private readonly Dictionary<string, Gauge> _nodeExtraInfo = new Dictionary<string, Gauge>();
        private readonly MetricServer _server;
        private readonly CollectorRegistry _registry;
        private readonly bool _exportNodeDiskInfo;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pveHostsAndPortHA"></param>
        /// <param name="pveUsername"></param>
        /// <param name="pvePassword"></param>
        /// <param name="pveApiToken"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="url"></param>
        /// <param name="prefix"></param>
        /// <param name="exportNodeDiskInfo"></param>
        public PrometheusExporter(string pveHostsAndPortHA,
                                  string pveUsername,
                                  string pvePassword,
                                  string pveApiToken,
                                  ILoggerFactory loggerFactory,
                                  string host,
                                  int port,
                                  string url,
                                  string prefix,
                                  bool exportNodeDiskInfo)
                                  : this(Prometheus.Metrics.NewCustomRegistry(), prefix, exportNodeDiskInfo)
        {
            _registry.AddBeforeCollectCallback(async () =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var client = ClientHelper.GetClientFromHA(pveHostsAndPortHA);
                client.LoggerFactory = loggerFactory;
                if (string.IsNullOrWhiteSpace(pveApiToken)) { await client.Login(pveUsername, pvePassword); }
                await Collect(client);

                stopwatch.Stop();
            });

            _server = new MetricServer(host, port, url, _registry);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="prefix"></param>
        /// <param name="exportNodeDiskInfo"></param>
        public PrometheusExporter(CollectorRegistry registry, string prefix, bool exportNodeDiskInfo)
        {
            _registry = registry;
            _exportNodeDiskInfo = exportNodeDiskInfo;

            var metricFactory = Prometheus.Metrics.WithCustomRegistry(registry);

            //create gauges
            _up = metricFactory.CreateGauge($"{prefix}_up",
                                            "Proxmox VE Node/Storage/VM/CT-Status is online/running/available",
                                            new GaugeConfiguration { LabelNames = new[] { "Id" } });

            _nodeInfo = metricFactory.CreateGauge($"{prefix}_node_info",
                                                  "Node info",
                                                  new GaugeConfiguration
                                                  {
                                                      LabelNames = new[]
                                                        {
                                                            nameof(ClusterStatus.Id),
                                                            nameof(ClusterStatus.IpAddress),
                                                            nameof(ClusterStatus.Level),
                                                            nameof(ClusterStatus.Local),
                                                            nameof(ClusterStatus.Name),
                                                            nameof(ClusterStatus.NodeId),
                                                        }
                                                  });

            _nodeDiskWearout = metricFactory.CreateGauge($"{prefix}_node_disk_Wearout",
                                                         "Node disk wearout",
                                                         new GaugeConfiguration
                                                         {
                                                             LabelNames = new[]
                                                                 {
                                                                     nameof(NodeDiskList.Serial),
                                                                     "Node",
                                                                     nameof(NodeDiskList.Type),
                                                                     nameof(NodeDiskList.DevPath),
                                                                 }
                                                         });

            _nodeDiskHealth = metricFactory.CreateGauge($"{prefix}_node_disk_health",
                                                        "Node disk health",
                                                         new GaugeConfiguration
                                                         {
                                                             LabelNames = new[]
                                                                 {
                                                                     nameof(NodeDiskList.Serial),
                                                                     "Node",
                                                                     nameof(NodeDiskList.Type),
                                                                     nameof(NodeDiskList.DevPath),
                                                                 }
                                                         });

            _nodeDiskSmart = metricFactory.CreateGauge($"{prefix}_node_disk_smart",
                                                       "Node disk smart",
                                                        new GaugeConfiguration
                                                        {
                                                            LabelNames = new[]
                                                                {
                                                                    nameof(NodeDiskList.Serial),
                                                                    "Node",
                                                                    nameof(NodeDiskList.Type),
                                                                    nameof(NodeDiskList.DevPath),
                                                                }
                                                        });
            //                                                       new GaugeConfiguration { LabelNames = new[] { "serial", "node", "type", "dev", "name" } });

            _clusterInfo = metricFactory.CreateGauge($"{prefix}_cluster_info",
                                                     "Cluster info",
                                                     new GaugeConfiguration
                                                     {
                                                         LabelNames = new[]
                                                        {
                                                            nameof(ClusterStatus.Id),
                                                            nameof(ClusterStatus.Nodes),
                                                            nameof(ClusterStatus.Quorate),
                                                            nameof(ClusterStatus.Version),
                                                        }
                                                     });

            _versionInfo = metricFactory.CreateGauge($"{prefix}_version_info",
                                                     "Proxmox VE version info",
                                                     new GaugeConfiguration
                                                     {
                                                         LabelNames = new[]
                                                        {
                                                            nameof(NodeVersion.Release),
                                                            nameof(NodeVersion.RepositoryId),
                                                            nameof(NodeVersion.Version),
                                                        }
                                                     });

            _guestInfo = metricFactory.CreateGauge($"{prefix}_guest_info",
                                                   "VM/CT info",
                                                   new GaugeConfiguration
                                                   {
                                                       LabelNames = new[]
                                                        {
                                                            nameof(IClusterResourceVm.VmId),
                                                            nameof(IClusterResourceVm.Node),
                                                            nameof(IClusterResourceVm.Name),
                                                            nameof(IClusterResourceVm.Type),
                                                        }
                                                   });

            _storageInfo = metricFactory.CreateGauge($"{prefix}_storage_info",
                                                     "Storage info",
                                                     new GaugeConfiguration
                                                     {
                                                         LabelNames = new[]
                                                            {
                                                                nameof(IClusterResourceStorage.Id),
                                                                nameof(IClusterResourceStorage.Node),
                                                                nameof(IClusterResourceStorage.Storage),
                                                                nameof(IClusterResourceStorage.Shared),
                                                            }
                                                     });

            // StorageDef = metricFactory.CreateGauge($"{prefix}_storage_def",
            //                                             "Storage info 1",
            //                                             new GaugeConfiguration {LabelNames = new[] { "nodes", "storage", "type", "shared", "content" }});

            CreateGauges(metricFactory, prefix);
        }

        /// <summary>
        /// Stop service
        /// </summary>
        public void Start() => _server?.Start();

        /// <summary>
        /// Stop service
        /// </summary>
        public void Stop() => _server?.Stop();

        private void CreateGauges(MetricFactory metricFactory, string prefix)
        {
            var defs = new (string propertyName, string name, string description)[]
            {
                ( nameof(IClusterResourceVm.HostMemoryUsage), "host_memory_usage_bytes", "Host memory usage" ),
                ( nameof(IClusterResourceVm.DiskSize), "disk_size_bytes", "Size of storage device" ),
                ( nameof(IClusterResourceVm.DiskUsage) , "disk_usage_bytes", "Disk usage in bytes" ),
                ( nameof(IClusterResourceVm.MemorySize), "memory_size_bytes", "Size of memory" ),
                ( nameof(IClusterResourceVm.MemoryUsage), "memory_usage_bytes", "Memory usage in bytes" ),
                ( nameof(IClusterResourceVm.NetOut) , "network_transmit_bytes", "Number of bytes transmitted over the network" ),
                ( nameof(IClusterResourceVm.NetIn) , "network_receive_bytes", "Number of bytes received over the network" ),
                ( nameof(IClusterResourceVm.DiskWrite), "disk_write_bytes", "Number of bytes written to storage" ),
                ( nameof(IClusterResourceVm.DiskRead), "disk_read_bytes", "Number of bytes read from storage" ),
                ( nameof(IClusterResourceVm.CpuUsagePercentage) , "cpu_usage_ratio",$"CPU usage (value between 0.0 and {prefix}_cpu_usage_limit)" ),
                ( nameof(IClusterResourceVm.CpuSize) , "cpu_usage_limit", "Maximum allowed CPU usage" ),
                ( nameof(IClusterResourceVm.Uptime), "uptime_seconds", "Number of seconds since the last boot" ),
            };

            foreach (var (propertyName, name, description) in defs)
            {
                _resourceInfo.Add(propertyName,
                                  metricFactory.CreateGauge($"{prefix}_{name}",
                                  description,
                                  new GaugeConfiguration
                                  {
                                      LabelNames = new[]
                                    {
                                        nameof(IClusterResourceVm.Id),
                                        nameof(IClusterResourceVm.Node),
                                    }
                                  }));
            }

            var defs2 = new (string Key, string Description)[]
            {
                ( "actual" , "Balloon memory actual" ),
                ( "max_mem" , "Balloon memory max" ),
                ( "last_update" , "Balloon memory last update" ),
            };

            foreach (var (Key, Description) in defs2)
            {
                _guestBalloonInfo.Add(Key,
                        metricFactory.CreateGauge($"{prefix}_balloon_{Key}_bytes",
                                                  Description,
                                                  new GaugeConfiguration { LabelNames = new[] { "id", "node" } }));
            }

            var defs3 = new (string key, string description)[]
            {
                ( "load_avg1", "Node load avg1" ),
                ( "load_avg5", "Node load avg5" ),
                ( "load_avg15", "Node load avg15" ),
                ( "uptime_seconds", "Number of seconds since the last boot" ),
            };

            foreach (var (key, description) in defs3)
            {
                _nodeExtraInfo.Add(key,
                       metricFactory.CreateGauge($"{prefix}_node_{key}",
                                                 description,
                                                 new GaugeConfiguration
                                                 {
                                                     LabelNames = new[]
                                                      {
                                                          nameof(IClusterResourceVm.Node),
                                                      }
                                                 }));
            }

            var defs4 = new (string key, string description)[]
            {
                ( "memory_used", "Node memory used" ),
                ( "memory_total", "Node memory total" ),
                ( "memory_free", "Node memory free" ),

                ( "swap_used", "Node swap used" ),
                ( "swap_total", "Node swap total" ),
                ( "swap_free", "Node swap free" ),

                ( "root_fs_used", "Node root fs used" ),
                ( "root_fs_total", "Node root fs total" ),
                ( "root_fs_free", "Node root fs free" ),
            };

            foreach (var (key, description) in defs4)
            {
                _nodeExtraInfo.Add(key,
                       metricFactory.CreateGauge($"{prefix}_node_{key}_bytes",
                                                 description,
                                                 new GaugeConfiguration
                                                 {
                                                     LabelNames = new[]
                                                      {
                                                          nameof(IClusterResourceVm.Node),
                                                      }
                                                 }));
            }
        }

        /// <summary>
        /// Collect data
        /// </summary>
        /// <param name="client"></param>
        public async Task Collect(PveClient client)
        {
            //todo fix in new version use decode model
            SetGauge(_versionInfo, await client.Version.Get());

            var formatInfo = new NumberFormatInfo
            {
                NumberDecimalSeparator = "."
            };

            foreach (var item in await client.Cluster.Status.Get())
            {
                switch (item.Type)
                {
                    case "node":
                        _up.WithLabels(item.Id).Set(item.IsOnline ? 1 : 0);
                        SetGauge(_nodeInfo, item);

                        if (item.IsOnline)
                        {
                            var status = await client.Nodes[item.Name].Status.Get();
                            var loadAvg = status.LoadAvg.ToArray();

                            foreach (var (key, gauge) in _nodeExtraInfo)
                            {
                                double value = key switch
                                {
                                    "uptime_seconds" => status.Uptime,

                                    "memory_used" => status.Memory.Used,
                                    "memory_total" => status.Memory.Total,
                                    "memory_free" => status.Memory.Free,

                                    "swap_used" => status.Swap.Used,
                                    "swap_total" => status.Swap.Total,
                                    "swap_free" => status.Swap.Free,

                                    "root_fs_used" => status.RootFs.Used,
                                    "root_fs_total" => status.RootFs.Total,
                                    "root_fs_free" => status.RootFs.Free,

                                    "load_avg1" => double.Parse(loadAvg[0], formatInfo),
                                    "load_avg5" => double.Parse(loadAvg[1], formatInfo),
                                    "load_avg15" => double.Parse(loadAvg[2], formatInfo),

                                    _ => 0.0,
                                };

                                gauge.WithLabels(item.Name).Set(value);
                            }

                            if (_exportNodeDiskInfo)
                            {
                                //disk info
                                foreach (var disk in await client.Nodes[item.Name].Disks.List.Get())
                                {
                                    var labelValues = new string[] { disk.Serial, item.Name, disk.Type, disk.DevPath };

                                    if (disk.Wearout != "N/A")
                                    {
                                        _nodeDiskHealth.WithLabels(labelValues).Set(double.Parse(disk.Wearout));
                                    }

                                    _nodeDiskWearout.WithLabels(labelValues).Set(disk.Health == "PASSED" ? 1 : 0);

                                    //smart
                                    var smart = await client.Nodes[item.Name].Disks.Smart.Get(disk.DevPath);
                                    foreach (var attribute in smart.Attributes)
                                    {
                                        var labelValuesAttr = new List<string>(labelValues)
                                        {
                                            attribute.Name
                                        };

                                        _nodeDiskSmart.WithLabels(labelValuesAttr.ToArray()).Set(attribute.Value);
                                    }
                                }
                            }
                        }
                        break;

                    case "cluster":
                        _up.WithLabels($"cluster/{item.Name}").Set(item.Quorate);
                        SetGauge(_clusterInfo, item);
                        break;

                    default: break;
                }
            }

            //foreach (var item in client.Storage.Index().ToEnumerable()) { SetGauge1(StorageDef, item); }

            foreach (var item in (await client.Cluster.Resources.Get()).CalculateHostUsage())
            {
                switch (item.ResourceType)
                {
                    case ClusterResourceType.Vm:
                        _up.WithLabels(item.Id).Set(item.IsRunning ? 1 : 0);
                        SetGauge(_guestInfo, item);

                        if (item.IsRunning && item.VmType == VmType.Qemu)
                        {
                            var data = (await client.Nodes[item.Node]
                                                    .Qemu[item.VmId]
                                                    .Monitor.Monitor("info balloon"))
                                            .Response.data as string;

                            if (data.StartsWith(KeyBalloon))
                            {
                                //split data
                                var dataIb = data[(data.IndexOf(KeyBalloon) + KeyBalloon.Length)..]
                                                .Split(' ')
                                                .Select(a => new
                                                {
                                                    Name = a.Split("=")[0],
                                                    Value = double.Parse(a.Split("=")[1], formatInfo)
                                                });

                                foreach (var gbi in _guestBalloonInfo)
                                {
                                    var row = dataIb.FirstOrDefault(a => a.Name == gbi.Key);
                                    if (row != null)
                                    {
                                        gbi.Value.WithLabels(new[] { item.VmId.ToString(), item.Node })
                                                 .Set(row.Value * 1024 * 1024);
                                    }
                                }
                            }
                        }

                        foreach (var item1 in _resourceInfo)
                        {
                            item1.Value.WithLabels(GetValues(item1.Value, item))
                                       .Set(GetValue<double>(item, item1.Key));
                        }
                        break;

                    case ClusterResourceType.Storage:
                        _up.WithLabels(item.Id).Set(item.IsAvailable ? 1 : 0);
                        SetGauge(_storageInfo, item);
                        break;

                    default: break;
                }
            }
        }

        private static void SetGauge(Gauge gauge, dynamic values) => gauge.WithLabels(GetValues(gauge, values)).Set(1);

        private static string[] GetValues(Gauge gauge, dynamic obj)
            => gauge.LabelNames.Select(a => GetValue<object>(obj, a) + "")
                               .Cast<string>()
                               .ToArray();

        private static T GetValue<T>(object obj, string propertyName)
            => (T)Convert.ChangeType(obj.GetType().GetProperty(propertyName).GetValue(obj), typeof(T));
    }
}