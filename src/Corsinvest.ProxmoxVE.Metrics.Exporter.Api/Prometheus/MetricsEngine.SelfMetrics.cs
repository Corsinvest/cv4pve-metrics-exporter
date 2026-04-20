/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    private Gauge _scrapeDuration = null!;
    private Gauge _lastSuccessTimestamp = null!;
    private Counter _scrapeErrors = null!;

    private void InitSelfMetrics(MetricFactory mf)
    {
        _scrapeDuration = mf.CreateGauge("cv4pve_scrape_duration_seconds", "Duration of the last scrape in seconds");
        _lastSuccessTimestamp = mf.CreateGauge("cv4pve_scrape_last_success_timestamp_seconds", "Unix timestamp of the last successful scrape");
        _scrapeErrors = mf.CreateCounter("cv4pve_scrape_errors_total",
                                         "Total number of errors encountered during scrapes",
                                         new CounterConfiguration { LabelNames = ["section"] });
    }

    private void TrackErrors(string section, params Task[] tasks)
    {
        var failed = tasks.Count(t => t.IsFaulted || t.IsCanceled);
        if (failed > 0) { _scrapeErrors.WithLabels(section).Inc(failed); }
    }
}
