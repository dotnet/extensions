// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

public partial class DiskBasedResponseCache
{
    /// <summary>
    /// An enum representing the mode in which the cache is operating.
    /// </summary>
    internal enum CacheMode
    {
        /// <summary>
        /// In this mode, the cache is disabled. All requests bypass the cache and are forwarded online.
        /// </summary>
        Disabled,

        /// <summary>
        /// In this mode, the cache is enabled. Requests are handled by the cache first. If a cached response is not
        /// available, then the request is forwarded online.
        /// </summary>
        Enabled,

        /// <summary>
        /// In this mode, the cache is enabled. However, requests are never forwarded online. Instead if a cached response
        /// is not available, then an exception is thrown. Additionally in this mode, the cache is considered frozen (or
        /// read only) which means that all the cache artifacts (including expired entries) are preserved as is on disk.
        /// </summary>
        EnabledOfflineOnly
    }
}
