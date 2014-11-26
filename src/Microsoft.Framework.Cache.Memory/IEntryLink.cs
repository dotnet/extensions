// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.Framework.Cache.Memory
{
    /// <summary>
    /// Used to flow expiration information from one entry to another. Triggers and minimum absolute expiration will
    /// be copied from the dependent entry to the parent entry. The parent entry will not expire if the
    /// dependent entry is removed manually, removed due to memory pressure, or expires due to sliding expiration.
    /// </summary>
    [AssemblyNeutral]
    public interface IEntryLink
    {
        /// <summary>
        /// Gets the minimum absolute expiration for all dependent entries, or null if not set.
        /// </summary>
        DateTimeOffset? AbsoluteExpiration { get; }

        /// <summary>
        /// Gets all the triggers from the dependent entries.
        /// </summary>
        IEnumerable<IExpirationTrigger> Triggers { get; }

        /// <summary>
        /// Adds triggers from a dependent entries.
        /// </summary>
        /// <param name="triggers"></param>
        void AddExpirationTriggers(IList<IExpirationTrigger> triggers);

        /// <summary>
        /// Sets the absolute expiration for from a dependent entry. The minimum value across all dependent entries
        /// will be used.
        /// </summary>
        /// <param name="absoluteExpiration"></param>
        void SetAbsoluteExpiration(DateTimeOffset absoluteExpiration);
    }
}