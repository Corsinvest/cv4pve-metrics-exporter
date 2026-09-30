/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using System.Reflection;
using Corsinvest.ProxmoxVE.Api;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    // Login state of PveClientBase has private setters: copied so the per-call client is logged in too.
    private static readonly PropertyInfo CsrfProperty = typeof(PveClientBase).GetProperty(nameof(PveClientBase.CSRFPreventionToken))!;
    private static readonly PropertyInfo AuthCookieProperty = typeof(PveClientBase).GetProperty(nameof(PveClientBase.PVEAuthCookie))!;

    /// <summary>
    /// A client for one API call. PveClientBase (Corsinvest.ProxmoxVE.Api 9.2.3) returns its shared
    /// <c>LastResult</c> field, so concurrent calls on one client can receive each other's answers. Each call
    /// gets its own client, sharing the HTTP connection, the API token or the login ticket.
    /// </summary>
    private PveClient Fork(PveClient client)
    {
        var fork = new PveClient(client.Host, client.Port, client.GetHttpClient())
        {
            ApiToken = client.ApiToken,
            Timeout = client.Timeout,
            LoggerFactory = client.LoggerFactory,
        };

        CsrfProperty.SetValue(fork, client.CSRFPreventionToken);
        AuthCookieProperty.SetValue(fork, client.PVEAuthCookie);

        if (_settings.ApiInstrumentation) { fork.RequestCompleted += OnApiRequestCompleted; }
        return fork;
    }
}
