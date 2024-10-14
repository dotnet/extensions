// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Diagnostics.Logging.Sampling;
internal class HttpRequestRatioBasedSampler : LoggerSampler
{
    private readonly int _sampleRate;
    private readonly IHttpContextAccessor _accessor;

    public HttpRequestRatioBasedSampler(double sampleRate, IHttpContextAccessor accessor)
    {
        _sampleRate = (int)(sampleRate * 100);
        _accessor = accessor;
    }

    public override bool ShouldSample(SamplingParameters parameters)
    {
        Span<byte> traceIdBytes = stackalloc byte[16];
        return Math.Abs(GetLowerLong(_accessor.HttpContext?.TraceIdentifier)) < sampleRate;
    }

    private static long GetLowerLong(ReadOnlySpan<byte> bytes)
    {
        long result = 0;
        for (var i = 0; i < 8; i++)
        {
            result <<= 8;
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
            result |= bytes[i] & 0xff;
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
        }

        return result;
    }
}
