/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Net;
using System.Net.Sockets;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Shared;
using Corsinvest.ProxmoxVE.Metrics.Exporter.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using PrometheusSettings = Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus.Settings;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Tests;

/// <summary>The HTTP endpoint: each request reads the cluster before answering.</summary>
public class PrometheusServerTests
{
    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static (PrometheusServer Server, string Url) Start(Func<Task<PveClient>> clientFactory)
    {
        var settings = PrometheusSettings.Fast();
        settings.Port = FreePort();
        var server = new PrometheusServer(clientFactory, settings, NullLoggerFactory.Instance);
        server.Start();
        return (server, $"http://localhost:{settings.Port}/metrics/");
    }

    [Fact]
    public async Task First_scrape_already_has_the_cluster_metrics()
    {
        var pve = FakePve.TwoNodeCluster();
        var (server, url) = Start(() => Task.FromResult(pve.CreateClient()));
        try
        {
            using var http = new HttpClient();
            var body = await http.GetStringAsync(url);

            Assert.Contains("cv4pve_up{id=\"node/pve01\",type=\"node\"} 1", body);
        }
        finally { server.Stop(); }
    }

    [Fact]
    public async Task Each_scrape_answers_with_the_data_read_for_it()
    {
        var pve = FakePve.TwoNodeCluster();
        var (server, url) = Start(() => Task.FromResult(pve.CreateClient()));
        try
        {
            using var http = new HttpClient();
            await http.GetStringAsync(url);
            pve.Resource("qemu/100")["name"] = "renamed";
            var body = await http.GetStringAsync(url);

            Assert.Contains("name=\"renamed\"", body);
        }
        finally { server.Stop(); }
    }

    [Fact]
    public async Task Login_failure_answers_503_and_the_server_keeps_running()
    {
        var fail = true;
        var pve = FakePve.TwoNodeCluster();
        var (server, url) = Start(() => fail
                                        ? throw new PveException("Authentication failed for host pve01:8006: authentication failure")
                                        : Task.FromResult(pve.CreateClient()));
        try
        {
            using var http = new HttpClient();
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await http.GetAsync(url)).StatusCode);

            fail = false;
            var response = await http.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("cv4pve_up{id=\"node/pve01\"", await response.Content.ReadAsStringAsync());
        }
        finally { server.Stop(); }
    }
}
