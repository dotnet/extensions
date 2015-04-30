// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.Caching.Memory
{
    // TODO: Granularity?
    /// <summary>
    /// Specifies how items are prioritized for preservation during a memory pressure triggered cleanup.
    /// </summary>
    public enum CachePreservationPriority
    {
        Low,
        Normal,
        High,
        NeverRemove,
    }
}