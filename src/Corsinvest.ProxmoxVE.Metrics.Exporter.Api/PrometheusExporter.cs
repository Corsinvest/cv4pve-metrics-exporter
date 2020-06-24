using System.Collections.Generic;
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

        private readonly Gauge Up;
        private readonly Gauge NodeInfo;
        private readonly Gauge ClusterInfo;
        private readonly Gauge VersionInfo;
        private readonly Gauge GuestInfo;
        private readonly Gauge StorageInfo;
        private readonly Gauge StorageDef;
        private readonly Dictionary<string, Gauge> DicResourceInfo = new Dictionary<string, Gauge>();
        private readonly Dictionary<string, Gauge> DicGuestBalloonInfo = new Dictionary<string, Gauge>();
        private readonly Dictionary<string, Gauge> DicNodeExtraInfo = new Dictionary<string, Gauge>();
        private MetricServer _server;

        /// <summary>
        /// Start
        /// </summary>
        /// <param name="pveHostsAndPortHA"></param>
        /// <param name="pveUsername"></param>
        /// <param name="pvePassword"></param>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="url"></param>
        /// <param name="prefix"></param>
        public PrometheusExporter(string pveHostsAndPortHA,
                                  string pveUsername,
                                  string pvePassword,
                                  string host,
                                  int port,
                                  string url,
                                  string prefix)
        {
            Prometheus.Metrics.DefaultRegistry.AddBeforeCollectCallback(() =>
            {
                var pveClient = ClientHelper.GetClientFromHA(pveHostsAndPortHA, null);
                pveClient.Login(pveUsername, pvePassword);
                Collect(pveClient);
            });

            _server = new MetricServer(host, port, url);

            //create gauges
            Up = Prometheus.Metrics.CreateGauge($"{prefix}_up",
                        "Proxmox VE Node/Storage/VM/CT-Status is online/running/aviable",
                        new GaugeConfiguration { LabelNames = new[] { "id" } });

            NodeInfo = Prometheus.Metrics.CreateGauge($"{prefix}_node_info",
                                                        "Node info",
                                                        new GaugeConfiguration
                                                        {
                                                            LabelNames = new[] { "id", "ip", "level", "local", "name", "nodeid" }
                                                        });

            ClusterInfo = Prometheus.Metrics.CreateGauge($"{prefix}_cluster_info",
                                                         "Node info",
                                                         new GaugeConfiguration
                                                         {
                                                            LabelNames = new[] { "id", "nodes", "quorate", "version" }
                                                         });

            VersionInfo = Prometheus.Metrics.CreateGauge($"{prefix}_version_info",
                                                         "Proxmox VE version info",
                                                         new GaugeConfiguration
                                                         {
                                                            LabelNames = new[] { "release", "repoid", "version" }
                                                         });

            GuestInfo = Prometheus.Metrics.CreateGauge($"{prefix}_guest_info",
                                                        "VM/CT info",
                                                        new GaugeConfiguration
                                                        {
                                                            LabelNames = new[] { "id", "node", "name", "type" }
                                                        });

            StorageInfo = Prometheus.Metrics.CreateGauge($"{prefix}_storage_info",
                                                         "Storage info",
                                                         new GaugeConfiguration
                                                         {
                                                            LabelNames = new[] { "id", "node", "storage" }
                                                         });

            StorageDef = Prometheus.Metrics.CreateGauge($"{prefix}_storage_def",
                                                        "Storage info 1",
                                                        new GaugeConfiguration
                                                        {
                                                            LabelNames = new[] { "nodes", "storage", "type", "shared", "content" }
                                                        });

            CreateGauges(prefix);
        }

        private void CreateGauges(string prefix)
        {

            var defs = new (string Key, string Name, string Description)[]
            {
                ("maxdisk","disk_size_bytes","Size of storage device"),
                ("disk","disk_usage_bytes","Disk usage in bytes"),
                ("maxmem","memory_size_bytes","Size of memory"),
                ("mem","memory_usage_bytes","Memory usage in bytes"),
                ("netout","network_transmit_bytes","Number of bytes transmitted over the network"),
                ("netin","network_receive_bytes","Number of bytes received over the network"),
                ("diskwrite","disk_write_bytes","Number of bytes written to storage"),
                ("diskread","disk_read_bytes","Number of bytes read from storage"),
                ("cpu","cpu_usage_ratio",$"CPU usage (value between 0.0 and {prefix}_cpu_usage_limit)"),
                ("maxcpu","cpu_usage_limit","Maximum allowed CPU usage"),
                ("uptime","uptime_seconds","Number of seconds since the last boot"),
            };

            foreach (var item in defs)
            {
                DicResourceInfo.Add(item.Key,
                        Prometheus.Metrics.CreateGauge($"{prefix}_{item.Name}", item.Description,
                            new GaugeConfiguration { LabelNames = new[] { "id", "node" } }));
            }

            var defs2 = new (string Key, string Description)[]
            {
                ("actual","Balloon memory actual"),
                ("max_mem","Balloon memory max"),
                ("last_update","Balloon memory last update"),
            };

            foreach (var item in defs2)
            {
                DicGuestBalloonInfo.Add(item.Key,
                        Prometheus.Metrics.CreateGauge($"{prefix}_balloon_{item.Key}_bytes",
                                                       item.Description,
                                                       new GaugeConfiguration { LabelNames = new[] { "id", "node" } }));
            }

            var defs3 = new (string Key, string Description)[]
            {
                ("load_avg1","Node load avg1"),
                ("load_avg5","Node load avg5"),
                ("load_avg15","Node load avg15"),
            };

            foreach (var item in defs3)
            {
                DicNodeExtraInfo.Add(item.Key,
                       Prometheus.Metrics.CreateGauge($"{prefix}_node_{item.Key}", item.Description,
                                                      new GaugeConfiguration { LabelNames = new[] { "node" } }));
            }

            var defs4 = new (string Key, string Description)[]
            {
                ("memory_used","Node memory used"),
                ("memory_total","Node memory total"),
                ("memory_free","Node memory free"),
                ("swap_used","Node swap used"),
                ("swap_total","Node swap total"),
                ("swap_free","Node swap free"),
            };

            foreach (var item in defs4)
            {
                DicNodeExtraInfo.Add(item.Key,
                       Prometheus.Metrics.CreateGauge($"{prefix}_node_{item.Key}_bytes", item.Description,
                                                      new GaugeConfiguration { LabelNames = new[] { "node" } }));
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        public void Start() => _server.Start();

        /// <summary>
        /// Stop
        /// </summary>
        public void Stop() => _server.Stop();

        private void Collect(PveClient client)
        {
            SetGauge1(VersionInfo, client.Version.Version().Response.data);

            foreach (var item in client.Cluster.Status.GetStatus().ToEnumerable())
            {
                switch (item.type)
                {
                    case "node":
                        Up.WithLabels(item.id as string).Set(item.online);
                        SetGauge1(NodeInfo, item);

                        if (item.online == 1)
                        {
                            var node = item.name as string;
                            var data = client.Nodes[node].Status.Status().Response.data;

                            foreach (var item1 in DicNodeExtraInfo)
                            {
                                var value = 0.0;
                                switch (item1.Key)
                                {
                                    case "memory_used": value = data.memory.used; break;
                                    case "memory_total": value = data.memory.total; break;
                                    case "memory_free": value = data.memory.free; break;
                                    case "load_avg1": value = double.Parse(data.loadavg[0]); break;
                                    case "load_avg5": value = double.Parse(data.loadavg[1]); break;
                                    case "load_avg15": value = double.Parse(data.loadavg[2]); break;
                                    case "swap_used": value = data.swap.used; break;
                                    case "swap_total": value = data.swap.total; break;
                                    case "swap_free": value = data.swap.free; break;
                                    default: break;
                                }
                                item1.Value.WithLabels(node).Set(value);
                            }
                        }
                        break;

                    case "cluster":
                        Up.WithLabels($"cluster/{item.name}").Set(item.quorate);
                        SetGauge1(ClusterInfo, item);
                        break;

                    default: break;
                }
            }

            foreach (var item in client.Storage.Index().ToEnumerable()) { SetGauge1(StorageDef, item); }

            foreach (var item in client.Cluster.Resources.Resources().ToEnumerable())
            {
                switch (item.type)
                {
                    case "qemu":
                    case "lxc":
                        var isRunning = item.status as string == "running";

                        Up.WithLabels($"{item.id}").Set(isRunning ? 1 : 0);
                        SetGauge1(GuestInfo, item);

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
                                                    Value = double.Parse(a.Split("=")[1])
                                                });

                            foreach (var item1 in DicGuestBalloonInfo)
                            {
                                var row = data.Where(a => a.Name == item1.Key).FirstOrDefault();
                                if (row != null) { item1.Value.WithLabels(new[] { vmId, node }).Set(row.Value * 1024 * 1024); }
                            }
                        }
                        break;

                    case "storage":
                        Up.WithLabels($"{item.id}").Set((item.status == "available") ? 1 : 0);
                        SetGauge1(StorageInfo, item);
                        break;

                    default: break;
                }

                foreach (var item1 in DicResourceInfo)
                {
                    var value = DynamicHelper.GetValue(item, item1.Key);
                    if (value != null) { item1.Value.WithLabels(GetValues(item, item1.Value)).Set(value); }
                }
            }
        }

        private static void SetGauge1(Gauge gauge, dynamic values)
            => gauge.WithLabels(GetValues(values, gauge)).Set(1);

        private static string[] GetValues(dynamic obj, Gauge gauge)
            => gauge.LabelNames.Select(a => DynamicHelper.GetValue(obj, a) + "").Cast<string>().ToArray();

    }
}