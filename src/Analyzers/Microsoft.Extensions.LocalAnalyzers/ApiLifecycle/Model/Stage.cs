// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;

internal enum Stage
{
    /// <summary>
    /// Public interface can be changed without notice.
    /// </summary>
    Experimental,

    /// <summary>
    /// Public interface changes needs to be documented and follow deprecation policy.
    /// </summary>
    Stable,

    /// <summary>
    /// Public interface is still functional but no longer recommended.
    /// </summary>
    Obsolete
}
