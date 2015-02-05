// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50 || ASPNETCORE50
using Microsoft.Framework.Runtime;
#endif

namespace Microsoft.Framework.Cache.Memory
{
#if ASPNET50 || ASPNETCORE50
    [AssemblyNeutral]
#endif
    public enum EvictionReason
    {
        None,

        /// <summary>
        /// Manually
        /// </summary>
        Removed,

        /// <summary>
        /// Overwritten
        /// </summary>
        Replaced,

        /// <summary>
        /// Timed out
        /// </summary>
        Expired,

        /// <summary>
        /// Event
        /// </summary>
        Triggered,

        /// <summary>
        /// GC, overflow
        /// </summary>
        Capacity,
    }
}