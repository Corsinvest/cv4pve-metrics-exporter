/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

using Corsinvest.ProxmoxVE.Api;
using Prometheus;

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Prometheus;

public partial class MetricsEngine
{
    /// <summary>
    /// The series written by one successful read of a collector. After the read, <see cref="Prune{TChild}"/>
    /// removes the series of that collector the read did not write — a deleted guest, an old version, a
    /// renamed tag — so they stop being exported.
    /// </summary>
    private sealed class Series
    {
        private const char Separator = '\u0001';
        private readonly Dictionary<object, HashSet<string>> _written = [];

        private void Add(object metric, string[] labels)
        {
            if (!_written.TryGetValue(metric, out var keys)) { _written[metric] = keys = new(StringComparer.Ordinal); }
            keys.Add(string.Join(Separator, labels));
        }

        public void Set(Gauge gauge, double value, params string[] labels)
        {
            gauge.WithLabels(labels).Set(value);
            Add(gauge, labels);
        }

        /// <summary>
        /// Sets a counter to the value Proxmox VE reports. When the value goes down — the guest restarted or
        /// migrated and Proxmox VE counts again from zero — the series is recreated, which Prometheus sees as
        /// a counter reset.
        /// </summary>
        public void SetCounter(Counter counter, double value, params string[] labels)
        {
            var child = counter.WithLabels(labels);
            if (value < child.Value)
            {
                counter.RemoveLabelled(labels);
                child = counter.WithLabels(labels);
            }
            child.IncTo(value);
            Add(counter, labels);
        }

        /// <summary>Removes the series of <paramref name="metric"/> in scope that this read did not write.</summary>
        public void Prune<TChild>(Collector<TChild> metric, Func<string[], bool>? inScope = null)
            where TChild : ChildBase
        {
            var keys = _written.GetValueOrDefault(metric);
            foreach (var labels in metric.GetAllLabelValues().ToArray())
            {
                if ((inScope == null || inScope(labels)) && keys?.Contains(string.Join(Separator, labels)) != true)
                {
                    metric.RemoveLabelled(labels);
                }
            }
        }
    }

    /// <summary>Removes every series of <paramref name="metric"/> matching <paramref name="predicate"/>.</summary>
    private static void RemoveWhere<TChild>(Collector<TChild> metric, Func<string[], bool> predicate)
        where TChild : ChildBase
    {
        foreach (var labels in metric.GetAllLabelValues().Where(predicate).ToArray()) { metric.RemoveLabelled(labels); }
    }

    /// <summary>Calls that return a raw <see cref="Result"/> do not throw on an HTTP error: make them fail like the typed ones.</summary>
    private static async Task<Result> Checked(Task<Result> call)
    {
        var result = await call;
        return result.IsSuccessStatusCode && !result.InError()
                ? result
                : throw new PveResultException(result, result.IsSuccessStatusCode ? result.GetError() : result.ReasonPhrase);
    }
}
