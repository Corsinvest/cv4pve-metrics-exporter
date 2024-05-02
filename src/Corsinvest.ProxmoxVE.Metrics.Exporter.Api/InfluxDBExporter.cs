// /*
//  * SPDX-License-Identifier: GPL-3.0-only
//  * SPDX-FileCopyrightText: 2019 Copyright Corsinvest Srl
//  */

// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using Corsinvest.ProxmoxVE.Api;
// using Corsinvest.ProxmoxVE.Api.Extension;
// using Corsinvest.ProxmoxVE.Api.Extension.Utils;
// using Corsinvest.ProxmoxVE.Api.Shared.Models.Access;
// using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
// using Corsinvest.ProxmoxVE.Api.Shared.Models.Node;
// using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
// using Corsinvest.ProxmoxVE.Api.Shared.Utils;
// using InfluxDB.Collector;
// using JsonSubTypes;
// using Microsoft.Extensions.Logging;
// using static Corsinvest.ProxmoxVE.Api.PveClient.PveAccess;

// namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api;

// /// <summary>
// /// InfluxDB Exporter
// /// </summary>
// public class InfluxDBExporter
// {
//     private readonly string _pveHostsAndPortHA;
//     private readonly string _pveUsername;
//     private readonly string _pvePassword;
//     private readonly string _pveApiToken;
//     private readonly ILoggerFactory _loggerFactory;
//     private readonly string _url;
//     private readonly string _database;

//     /// <summary>
//     /// Constructor
//     /// </summary>
//     /// <param name="pveHostsAndPortHA"></param>
//     /// <param name="pveUsername"></param>
//     /// <param name="pvePassword"></param>
//     /// <param name="pveApiToken"></param>
//     /// <param name="loggerFactory"></param>
//     /// <param name="url"></param>
//     /// <param name="database"></param>
//     public InfluxDBExporter(string pveHostsAndPortHA,
//                             string pveUsername,
//                             string pvePassword,
//                             string pveApiToken,
//                             ILoggerFactory loggerFactory,
//                             string url,
//                             string database)
//     {
//         _pveHostsAndPortHA = pveHostsAndPortHA;
//         _pveUsername = pveUsername;
//         _pvePassword = pvePassword;
//         _pveApiToken = pveApiToken;
//         _loggerFactory = loggerFactory;
//         _url = url;
//         _database = database;
//     }

//     /// <summary>
//     /// Send async data
//     /// </summary>
//     /// <returns></returns>
//     public async Task SendAsync()
//     {
//         var stopwatch = new Stopwatch();
//         stopwatch.Start();

//         var client = ClientHelper.GetClientFromHA(_pveHostsAndPortHA);
//         client.LoggerFactory = _loggerFactory;
//         if (string.IsNullOrWhiteSpace(_pveApiToken))
//         {
//             await client.Login(_pveUsername, _pvePassword);
//         }
//         //await CollectAsync2(client);

//         stopwatch.Stop();
//     }

//     private async Task CollectAsync1x(PveClient client)
//     {
//         var collector = new CollectorConfiguration()
//                //               .Tag.With("host", Environment.GetEnvironmentVariable("COMPUTERNAME"))
//                //               .Tag.With("os", Environment.GetEnvironmentVariable("OS"))
//                //.Tag.With("process", Path.GetFileName(process.MainModule.FileName))
//                .Batch.AtInterval(TimeSpan.FromSeconds(10))
//                .WriteTo.InfluxDB(_url, _database)
//                .CreateCollector();

//         var resources = await client.Cluster.Resources.Get();

//         foreach (var node in await client.Nodes.Get())
//         {
//             collector.Write("system", new Dictionary<string, object>()
//             {
//                 {"object", "nodes"},
//                 {"host", node.Node},
//                 {"uptime", node.Uptime},
//                 {"status", node.Status},
//                 {"cpu", node.CpuUsagePercentage},
//                 {"cpus", node.CpuSize},
//                 {"mem", node.MemoryUsage},
//                 {"maxmem", node.MemorySize},
//             });

//             //storages
//             foreach (var storage in await client.Nodes[node.Node].Storage.Get())
//             {
//                 var data = new Dictionary<string, object>
//                 {
//                     { "active", storage.Active },
//                     { "avail", storage.Available },
//                     { "content", storage.Content },
//                     { "enabled", storage.Enabled },
//                     { "host", storage.Storage },
//                     { "nodename", node.Node },
//                     { "shared", storage.Shared },
//                     { "total", storage.Size },
//                     { "used", storage.Used }
//                 };

//                 if (!string.IsNullOrWhiteSpace(storage.Type)) { data.Add("type", storage.Type); }

//                 collector.Write("system", data);
//             }

//             //qemu/lxc
//             foreach (var vm in resources.Where(a => a.ResourceType == ClusterResourceType.Vm))
//             {
//                 VmBaseStatusCurrent status = vm.VmType switch
//                 {
//                     VmType.Lxc => await client.Nodes[node.Node].Lxc[vm.VmId].Status.Current.Get(),
//                     VmType.Qemu => await client.Nodes[node.Node].Qemu[vm.VmId].Status.Current.Get(),
//                     _ => throw new ArgumentOutOfRangeException(),
//                 };

//                 var data = new Dictionary<string, object>
//                 {
//                     {"vmid", status.VmId},
//                     {"host", node.Node},
//                     {"uptime", status.Uptime},
//                     {"status", status.Status},
//                     {"cpu", status.CpuUsagePercentage},
//                     {"cpus", status.CpuSize},
//                     {"diskread", status.DiskRead},
//                     {"diskwrite", status.DiskWrite},
//                     {"mem", status.MemoryUsage},
//                     {"maxmem", status.MemorySize},
//                     {"disk", status.DiskUsage},
//                     {"maxdisk", status.DiskSize},
//                     {"netin", status.NetIn},
//                     {"netout", status.NetOut},
//                     {"pid", status.Pid},
//                     {"lock", vm.IsLocked ? 1:0},
//                 };

//                 if (vm.VmType == VmType.Qemu)
//                 {
//                     var qmStatus = (VmQemuStatusCurrent)status;
//                     data.Add("balloon", qmStatus.Balloon);
//                     data.Add("qmstatus", qmStatus.Qmpstatus);
//                     data.Add("running-qemu", qmStatus.RunningQemu);
//                     data.Add("running-machine", qmStatus.RunningMachine);

//                     //uèdate data from agent
//                     try
//                     {
//                         var aa = await client.Nodes[node.Node].Qemu[vm.VmId].Agent.GetFsinfo.Get();
//                         data["disk"] = aa.Result.Select(a => a.UsedBytes).Sum();
//                         data["maxdisk"] = aa.Result.Select(a => a.TotalBytes).Sum();
//                     }
//                     catch { }
//                 }

//                 collector.Write("system", data);
//             }
//         }
//     }
// }