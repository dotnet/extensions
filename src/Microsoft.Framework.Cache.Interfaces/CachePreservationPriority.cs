// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50 || ASPNETCORE50
using Microsoft.Framework.Runtime;
#endif

namespace Microsoft.Framework.Cache.Memory
{
    // TODO: Granularity?
    /// <summary>
    /// Specifies how items are prioritized for preservation during a memory pressure triggered cleanup.
    /// </summary>
#if ASPNET50 || ASPNETCORE50
    [AssemblyNeutral]
#endif
    public enum CachePreservationPriority
    {
        Low,
        Normal,
        High,
        NeverRemove,
    }
}