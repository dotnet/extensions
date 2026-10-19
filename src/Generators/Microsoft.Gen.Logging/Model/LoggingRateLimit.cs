// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Gen.Logging.Model;

/// <summary>
/// The rate limit of a logging method, from the RateLimit attribute.
/// </summary>
internal sealed class LoggingRateLimit
{
    public int PermitLimit;
    public int WindowMilliseconds;
    public int SegmentsPerWindow;
}
