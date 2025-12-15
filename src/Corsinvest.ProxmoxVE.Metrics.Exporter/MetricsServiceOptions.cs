/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

internal sealed class MetricsServiceOptions
{
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? ApiToken { get; set; }
    public bool ValidateCertificate { get; set; }
    public string HttpHost { get; set; } = string.Empty;
    public int HttpPort { get; set; }
    public string HttpUrl { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    public bool ServiceMode { get; set; }
}
