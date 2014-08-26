// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;

namespace Microsoft.AspNet.MemoryCache.Infrastructure
{
    /// <summary>
    /// Abstracts the system clock to facilitate testing.
    /// </summary>
    public interface ISystemClock
    {
        // TODO: DateTime or DateTimeOffset? Security uses DTO.
        /// <summary>
        /// Retrieves the current system time in UTC.
        /// </summary>
        DateTime UtcNow { get; }
    }
}
