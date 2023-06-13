// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

/// <summary>
/// FaultInjection event metric counter key names.
/// </summary>
internal static class FaultInjectionEventMeterDimensions
{
    /// <summary>
    /// Client using fault injection library.
    /// </summary>
    public const string FaultInjectionGroupName = "FaultInjectionGroupName";

    /// <summary>
    /// Type of fault injected, e.g, latency, exception, etc.
    /// </summary>
    public const string FaultType = "FaultType";

    /// <summary>
    /// Value corresponding to injected fault, for example, NotFoundException, 200ms.
    /// </summary>
    public const string InjectedValue = "InjectedValue";

    /// <summary>
    /// The http content key.
    /// </summary>
    public const string HttpContentKey = "HttpContentKey";
}
