using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Helpers;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api
{
    /// <summary>
    /// Prometheus Exporter
    /// </summary>
    public class PrometheusExporter
    {
        /// <summary>
        /// Default host
        /// </summary>
        public static readonly string DEFAULT_HOST = "localhost";

        /// <summary>
        /// Default port
        /// </summary>
        public static readonly int DEFAULT_PORT = 9221;

        /// <summary>
        /// Dewfault url
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
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="url"></param>
        /// <param name="prefix"></param>
        /// <param name="exportNodeDiskInfo"></param>
        public PrometheusExporter(string pveHostsAndPortHA,
                                  string pveUsername,
                                  string pvePassword,
                                  string host,
                                  int port,
                                  string url,
                                  string prefix,
                                  bool exportNodeDiskInfo)
                                  : this(Prometheus.Metrics.NewCustomRegistry(), prefix, exportNodeDiskInfo)
        {
            _registry.AddBeforeCollectCallback(() =>
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var client = ClientHelper.GetClientFromHA(pveHostsAndPortHA, null);
                client.Login(pveUsername, pvePassword);
                Collect(client);

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
                                            "Proxmox VE Node/Storage/VM/CT-Status is online/running/aviable",
                                            new GaugeConfiguration { LabelNames = new[] { "id" } });

            _nodeInfo = metricFactory.CreateGauge($"{prefix}_node_info",
                                                  "Node info",
                                                  new GaugeConfiguration { LabelNames = new[] { "id", "ip", "level", "local", "name", "nodeid" } });

            _nodeDiskWearout = metricFactory.CreateGauge($"{prefix}_node_disk_Wearout",
                                                         "Node disk wearout",
                                                         new GaugeConfiguration { LabelNames = new[] { "serial", "node", "type", "dev" } });

            _nodeDiskHealth = metricFactory.CreateGauge($"{prefix}_node_disk_health",
                                                        "Node disk health",
                                                        new GaugeConfiguration { LabelNames = new[] { "serial", "node", "type", "dev" } });

            _nodeDiskSmart = metricFactory.CreateGauge($"{prefix}_node_disk_smart",
                                                       "Node disk smart",
                                                       new GaugeConfiguration { LabelNames = new[] { "serial", "node", "type", "dev", "name" } });

            _clusterInfo = metricFactory.CreateGauge($"{prefix}_cluster_info",
                                                     "Cluster info",
                                                     new GaugeConfiguration { LabelNames = new[] { "id", "nodes", "quorate", "version" } });

            _versionInfo = metricFactory.CreateGauge($"{prefix}_version_info",
                                                     "Proxmox VE version info",
                                                     new GaugeConfiguration { LabelNames = new[] { "release", "repoid", "version" } });

            _guestInfo = metricFactory.CreateGauge($"{prefix}_guest_info",
                                                   "VM/CT info",
                                                   new GaugeConfiguration { LabelNames = new[] { "id", "node", "name", "type" } });

            _storageInfo = metricFactory.CreateGauge($"{prefix}_storage_info",
                                                     "Storage info",
                                                     new GaugeConfiguration { LabelNames = new[] { "id", "node", "storage", "shared" } });

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
            var defs = new (string Key, string Name, string Description)[]
            {
                ( "maxdisk", "disk_size_bytes", "Size of storage device" ),
                ( "disk", "disk_usage_bytes", "Disk usage in bytes" ),
                ( "maxmem", "memory_size_bytes", "Size of memory" ),
                ( "mem", "memory_usage_bytes", "Memory usage in bytes" ),
                ( "netout", "network_transmit_bytes", "Number of bytes transmitted over the network" ),
                ( "netin", "network_receive_bytes", "Number of bytes received over the network" ),
                ( "diskwrite", "disk_write_bytes", "Number of bytes written to storage" ),
                ( "diskread", "disk_read_bytes", "Number of bytes read from storage" ),
                ( "cpu", "cpu_usage_ratio",$"CPU usage (value between 0.0 and {prefix}_cpu_usage_limit)" ),
                ( "maxcpu", "cpu_usage_limit", "Maximum allowed CPU usage" ),
                ( "uptime", "uptime_seconds", "Number of seconds since the last boot" ),
            };

            foreach (var (Key, Name, Description) in defs)
            {
                _resourceInfo.Add(Key,
                                  metricFactory.CreateGauge($"{prefix}_{Name}",
                                  Description,
                                  new GaugeConfiguration { LabelNames = new[] { "id", "node" } }));
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

            var defs3 = new (string Key, string Description)[]
            {
                ( "load_avg1", "Node load avg1" ),
                ( "load_avg5", "Node load avg5" ),
                ( "load_avg15", "Node load avg15" ),
            };

            foreach (var (Key, Description) in defs3)
            {
                _nodeExtraInfo.Add(Key,
                       metricFactory.CreateGauge($"{prefix}_node_{Key}",
                                                 Description,
                                                 new GaugeConfiguration { LabelNames = new[] { "node" } }));
            }

            var defs4 = new (string Key, string Description)[]
            {
                ( "memory_used", "Node memory used" ),
                ( "memory_total", "Node memory total" ),
                ( "memory_free", "Node memory free" ),
                ( "swap_used", "Node swap used" ),
                ( "swap_total", "Node swap total" ),
                ( "swap_free", "Node swap free" ),
            };

            foreach (var (Key, Description) in defs4)
            {
                _nodeExtraInfo.Add(Key,
                       metricFactory.CreateGauge($"{prefix}_node_{Key}_bytes",
                                                 Description,
                                                 new GaugeConfiguration { LabelNames = new[] { "node" } }));
            }
        }

        /// <summary>
        /// Collect data
        /// </summary>
        /// <param name="client"></param>
        public void Collect(PveClient client)
        {
            var aa = client.Version.Version();
            SetGauge(_versionInfo, client.Version.Version().Response.data);

            var formatinfo = new NumberFormatInfo
            {
                NumberDecimalSeparator = "."
            };

            foreach (var item in client.Cluster.Status.GetStatus().ToEnumerable())
            {
                switch (item.type)
                {
                    case "node":
                        _up.WithLabels(item.id as string).Set(item.online);
                        SetGauge(_nodeInfo, item);

                        if (item.online == 1)
                        {
                            var node = item.name as string;
                            var data = client.Nodes[node].Status.Status().Response.data;

                            foreach (var item1 in _nodeExtraInfo)
                            {
                                double value = item1.Key switch
                                {
                                    "memory_used" => data.memory.used,
                                    "memory_total" => data.memory.total,
                                    "memory_free" => data.memory.free,
                                    "load_avg1" => double.Parse(data.loadavg[0], formatinfo),
                                    "load_avg5" => double.Parse(data.loadavg[1], formatinfo),
                                    "load_avg15" => double.Parse(data.loadavg[2], formatinfo),
                                    "swap_used" => data.swap.used,
                                    "swap_total" => data.swap.total,
                                    "swap_free" => data.swap.free,
                                    _ => 0.0,
                                };
                                item1.Value.WithLabels(node).Set(value);
                            }

                            if (_exportNodeDiskInfo)
                            {
                                //disk info
                                foreach (var disk in client.Nodes[node].Disks.List.List().ToEnumerable())
                                {
                                    var labelValues = new string[] { disk.serial as string,
                                                                     node,
                                                                     disk.type as string,
                                                                     disk.devpath as string };

                                    var wearout = DynamicHelper.GetValue(disk, "wearout");
                                    if (wearout is double wearoutD) { _nodeDiskHealth.WithLabels(labelValues).Set(wearoutD); }

                                    _nodeDiskWearout.WithLabels(labelValues).Set((disk.health as string) == "PASSED" ? 1 : 0);

                                    //smart
                                    var smart = client.Nodes[node].Disks.Smart.Smart(disk.devpath as string).Response.data;
                                    if (DynamicHelper.GetValue(smart, "attributes") != null)
                                    {
                                        foreach (var attribute in smart.attributes)
                                        {
                                            var labelValuesAttr = new List<string>();
                                            labelValuesAttr.AddRange(labelValues);
                                            labelValuesAttr.Add(attribute.name as string);

                                            _nodeDiskSmart.WithLabels(labelValuesAttr.ToArray()).Set(attribute.value);
                                        }
                                    }
                                }
                            }
                        }
                        break;

                    case "cluster":
                        _up.WithLabels($"cluster/{item.name}").Set(item.quorate);
                        SetGauge(_clusterInfo, item);
                        break;

                    default: break;
                }
            }

            //foreach (var item in client.Storage.Index().ToEnumerable()) { SetGauge1(StorageDef, item); }

            foreach (var item in client.Cluster.Resources.Resources().ToEnumerable())
            {
                switch (item.type)
                {
                    case "qemu":
                    case "lxc":
                        var isRunning = item.status as string == "running";

                        _up.WithLabels($"{item.id}").Set(isRunning ? 1 : 0);
                        SetGauge(_guestInfo, item);

                        if (isRunning && item.type == "qemu")
                        {
                            var node = item.node as string;
                            var vmId = item.vmid + "" as string;
                            var data = (client.Nodes[node]
                                              .Qemu[vmId]
                                              .Monitor.Monitor("info balloon")
                                              .Response.data as string)
                                              .Split(" ")
                                              .Skip(1)
                                              .Select(a => new
                                              {
                                                  Name = a.Split("=")[0],
                                                  Value = double.Parse(a.Split("=")[1], formatinfo)
                                              });

                            foreach (var gbi in _guestBalloonInfo)
                            {
                                var row = data.Where(a => a.Name == gbi.Key).FirstOrDefault();
                                if (row != null) { gbi.Value.WithLabels(new[] { vmId, node }).Set(row.Value * 1024 * 1024); }
                            }
                        }
                        break;

                    case "storage":
                        _up.WithLabels($"{item.id}").Set((item.status == "available") ? 1 : 0);
                        SetGauge(_storageInfo, item);
                        break;

                    default: break;
                }

                foreach (var item1 in _resourceInfo)
                {
                    var value = DynamicHelper.GetValue(item, item1.Key);
                    if (value != null) { item1.Value.WithLabels(GetValues(item1.Value, item)).Set(value); }
                }
            }
        }

        private static void SetGauge(Gauge gauge, dynamic values) => gauge.WithLabels(GetValues(gauge, values)).Set(1);

        private static string[] GetValues(Gauge gauge, dynamic obj)
            => gauge.LabelNames.Select(a => DynamicHelper.GetValue(obj, a) + "").Cast<string>().ToArray();
    }
}