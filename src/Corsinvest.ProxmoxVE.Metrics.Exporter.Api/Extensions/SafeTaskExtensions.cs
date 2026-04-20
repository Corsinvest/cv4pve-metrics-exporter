/*
 * SPDX-License-Identifier: GPL-3.0-only
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 */

namespace Corsinvest.ProxmoxVE.Metrics.Exporter.Api.Extensions;

internal static class SafeTaskExtensions
{
    public static Task WhenAllSafe(params Task[] tasks)
        => Task.WhenAll(tasks.Select(async t => { try { await t; } catch { } }));

    public static T? ResultOrDefault<T>(this Task<T> task)
        => task.IsCompletedSuccessfully
            ? task.Result
            : default;
}
