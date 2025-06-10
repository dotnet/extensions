// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

internal sealed class RetryingLinuxUtilizationParser : ILinuxUtilizationParser
{
    private readonly ILinuxUtilizationParser _inner;
    private readonly TimeSpan _retryInterval = TimeSpan.FromMinutes(5);
    private readonly TimeProvider _timeProvider;
    private DateTimeOffset _lastFailure = DateTimeOffset.MinValue;
    private int _unavailable;

    public RetryingLinuxUtilizationParser(ILinuxUtilizationParser inner, TimeProvider timeProvider)
    {
        _inner = Throw.IfNull(inner);
        _timeProvider = Throw.IfNull(timeProvider);
    }

    public ulong GetAvailableMemoryInBytes() =>
        Retry(() => _inner.GetAvailableMemoryInBytes());

    public long GetCgroupCpuUsageInNanoseconds() =>
        Retry(() => _inner.GetCgroupCpuUsageInNanoseconds());

    public (long cpuUsageNanoseconds, long elapsedPeriods) GetCgroupCpuUsageInNanosecondsAndCpuPeriodsV2() =>
        Retry(() => _inner.GetCgroupCpuUsageInNanosecondsAndCpuPeriodsV2());

    public float GetCgroupLimitedCpus() =>
        Retry(() => _inner.GetCgroupLimitedCpus());

    public float GetCgroupLimitV2() =>
        Retry(() => _inner.GetCgroupLimitV2());

    public ulong GetHostAvailableMemory() =>
        Retry(() => _inner.GetHostAvailableMemory());

    public float GetHostCpuCount() =>
        Retry(() => _inner.GetHostCpuCount());

    public long GetHostCpuUsageInNanoseconds() =>
        Retry(() => _inner.GetHostCpuUsageInNanoseconds());

    public ulong GetMemoryUsageInBytes() =>
        Retry(() => _inner.GetMemoryUsageInBytes());

    public float GetCgroupRequestCpu() =>
        Retry(() => _inner.GetCgroupRequestCpu());

    public float GetCgroupRequestCpuV2() =>
        Retry(() => _inner.GetCgroupRequestCpuV2());

    public long GetCgroupPeriodsIntervalInMicroSecondsV2() =>
        Retry(() => _inner.GetCgroupPeriodsIntervalInMicroSecondsV2());

#pragma warning disable CS8603 // Possible null reference return. It will return 0 or 0.0f
    private T Retry<T>(Func<T> func)
    {
        if (Volatile.Read(ref _unavailable) == 1 && _timeProvider.GetUtcNow() - _lastFailure < _retryInterval)
        {
            return default;
        }

        try
        {
            var result = func();
            if (Volatile.Read(ref _unavailable) == 1)
            {
                _ = Interlocked.Exchange(ref _unavailable, 0);
            }

            return result;
        }
        catch (Exception ex) when (
            ex is FileNotFoundException ||
            ex is DirectoryNotFoundException ||
            ex is UnauthorizedAccessException)
        {
            _lastFailure = _timeProvider.GetUtcNow();
            _ = Interlocked.Exchange(ref _unavailable, 1);
            return default;
        }
    }
#pragma warning restore CS8603 // Possible null reference return.
}
