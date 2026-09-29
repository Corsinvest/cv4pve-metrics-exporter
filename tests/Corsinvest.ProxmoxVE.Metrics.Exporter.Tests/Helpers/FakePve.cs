/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Corsinvest.ProxmoxVE.Api;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Tests.Helpers;

/// <summary>
/// In-memory Proxmox VE API: answers each path with the JSON set in <see cref="Data"/>, shaped like the
/// real API (<c>{"data": …}</c>). Tests change the data between two collections to simulate the cluster.
/// </summary>
internal sealed class FakePve : HttpMessageHandler
{
    /// <summary>Response data by API path, e.g. <c>/cluster/resources</c>.</summary>
    public Dictionary<string, JsonNode?> Data { get; } = [];

    /// <summary>Paths answering with an HTTP error instead of their data.</summary>
    public Dictionary<string, HttpStatusCode> Failures { get; } = [];

    /// <summary>Every request received, as <c>METHOD /path</c>.</summary>
    public List<string> Calls { get; } = [];

    public PveClient CreateClient()
        => new("pve01", 8006, new HttpClient(this)) { ApiToken = "metrics@pve!metrics=00000000-0000-0000-0000-000000000000" };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri!.AbsolutePath["/api2/json".Length..];
        lock (Calls) { Calls.Add($"{request.Method} {path}"); }

        if (Failures.TryGetValue(path, out var status))
        {
            return Task.FromResult(Json(status, """{"data":null}"""));
        }

        return Task.FromResult(Data.TryGetValue(path, out var data)
                                ? Json(HttpStatusCode.OK, new JsonObject { ["data"] = data?.DeepClone() }.ToJsonString())
                                : Json(HttpStatusCode.NotImplemented, """{"data":null}"""));
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public JsonArray Resources => (JsonArray)Data["/cluster/resources"]!;
    public JsonArray Status => (JsonArray)Data["/cluster/status"]!;

    /// <summary>The entry of <c>/cluster/resources</c> with this id.</summary>
    public JsonObject Resource(string id) => Resources.OfType<JsonObject>().Single(a => (string?)a["id"] == id);

    public void RemoveResource(string id) => Resources.Remove(Resource(id));

    /// <summary>
    /// Two online nodes, pve01 and pve02, with one VM and one container each, a local and a shared
    /// storage, one replication job, no HA resource and every guest covered by a backup job.
    /// </summary>
    public static FakePve TwoNodeCluster()
    {
        var pve = new FakePve();

        pve.Data["/cluster/status"] = JsonNode.Parse("""
            [
              { "type": "cluster", "id": "cluster", "name": "pve-cluster", "nodes": 2, "quorate": 1, "version": 3 },
              { "type": "node", "id": "node/pve01", "name": "pve01", "online": 1, "ip": "10.0.0.11", "level": "c", "local": 1, "nodeid": 1 },
              { "type": "node", "id": "node/pve02", "name": "pve02", "online": 1, "ip": "10.0.0.12", "level": "c", "local": 0, "nodeid": 2 }
            ]
            """);

        pve.Data["/cluster/resources"] = JsonNode.Parse("""
            [
              { "id": "node/pve01", "type": "node", "node": "pve01", "status": "online", "maxcpu": 16, "cpu": 0.1, "maxmem": 68719476736, "mem": 17179869184, "uptime": 100000, "level": "c" },
              { "id": "node/pve02", "type": "node", "node": "pve02", "status": "online", "maxcpu": 8, "cpu": 0.2, "maxmem": 34359738368, "mem": 8589934592, "uptime": 90000, "level": "c" },
              { "id": "qemu/100", "type": "qemu", "vmid": 100, "name": "web01", "node": "pve01", "status": "running", "maxcpu": 4, "cpu": 0.25,
                "maxmem": 8589934592, "mem": 4294967296, "maxdisk": 34359738368, "disk": 0, "uptime": 5000,
                "netin": 1000, "netout": 2000, "diskread": 3000, "diskwrite": 4000, "tags": "web;prod", "template": 0 },
              { "id": "lxc/101", "type": "lxc", "vmid": 101, "name": "db01", "node": "pve01", "status": "running", "maxcpu": 2, "cpu": 0.5,
                "maxmem": 4294967296, "mem": 1073741824, "maxdisk": 8589934592, "disk": 2147483648, "uptime": 6000,
                "netin": 10, "netout": 20, "diskread": 30, "diskwrite": 40, "tags": "", "template": 0 },
              { "id": "qemu/200", "type": "qemu", "vmid": 200, "name": "test01", "node": "pve02", "status": "stopped", "maxcpu": 2, "cpu": 0,
                "maxmem": 2147483648, "mem": 0, "maxdisk": 17179869184, "disk": 0, "uptime": 0,
                "netin": 0, "netout": 0, "diskread": 0, "diskwrite": 0, "template": 0 },
              { "id": "storage/pve01/local", "type": "storage", "node": "pve01", "storage": "local", "status": "available", "shared": 0,
                "content": "vztmpl,iso,backup", "plugintype": "dir", "maxdisk": 107374182400, "disk": 10737418240 },
              { "id": "storage/pve01/nfs01", "type": "storage", "node": "pve01", "storage": "nfs01", "status": "available", "shared": 1,
                "content": "images", "plugintype": "nfs", "maxdisk": 1099511627776, "disk": 549755813888 },
              { "id": "storage/pve02/nfs01", "type": "storage", "node": "pve02", "storage": "nfs01", "status": "available", "shared": 1,
                "content": "images", "plugintype": "nfs", "maxdisk": 1099511627776, "disk": 549755813888 }
            ]
            """);

        pve.Data["/cluster/ha/resources"] = new JsonArray();
        pve.Data["/cluster/ha/status/manager_status"] = JsonNode.Parse("""
            { "manager_status": {}, "quorum": { "node": "pve01", "quorate": 1 } }
            """);
        pve.Data["/cluster/backup-info/not-backed-up"] = new JsonArray();

        foreach (var (node, memUsed) in new[] { ("pve01", 17179869184L), ("pve02", 8589934592L) })
        {
            pve.Data[$"/nodes/{node}/status"] = JsonNode.Parse($$"""
                {
                  "uptime": 100000,
                  "loadavg": [ "0.50", "0.40", "0.30" ],
                  "memory": { "used": {{memUsed}}, "total": 68719476736, "free": 1 },
                  "swap": { "used": 0, "total": 8589934592, "free": 8589934592 },
                  "rootfs": { "used": 10737418240, "total": 107374182400, "free": 1, "avail": 1 }
                }
                """);
            pve.Data[$"/nodes/{node}/version"] = JsonNode.Parse("""{ "version": "9.0.10", "release": "9.0", "repoid": "0123456789abcdef" }""");
            pve.Data[$"/nodes/{node}/subscription"] = JsonNode.Parse("""{ "status": "active", "level": "c", "nextduedate": "2027-01-31" }""");
            pve.Data[$"/nodes/{node}/replication"] = new JsonArray();
            pve.Data[$"/nodes/{node}/disks/list"] = JsonNode.Parse("""
                [ { "devpath": "/dev/sda", "serial": "SSD0001", "type": "ssd", "health": "PASSED", "wearout": 97, "size": 480103981056 } ]
                """);
        }

        pve.Data["/nodes/pve01/replication"] = JsonNode.Parse("""
            [ { "id": "100-0", "type": "local", "source": "pve01", "target": "pve02", "guest": "100",
                "duration": 8.5, "last_sync": 1790000000, "next_sync": 1790000900, "fail_count": 0 } ]
            """);

        pve.Data["/nodes/pve01/qemu/100/monitor"] = JsonValue.Create("balloon: actual=6144 max_mem=8192");

        return pve;
    }
}
