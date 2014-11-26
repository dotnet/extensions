// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Runtime;

namespace Microsoft.Framework.Expiration.Interfaces
{
    [AssemblyNeutral]
    public interface IExpirationTrigger
    {
        /// <summary>
        /// Checked each time the key is accessed in the cache.
        /// </summary>
        bool IsExpired { get; }

        /// <summary>
        /// Indicates if this trigger will pro-actively trigger callbacks. Callbacks are still guaranteed to fire, eventually.
        /// </summary>
        bool ActiveExpirationCallbacks { get; }

        /// <summary>
        /// Registers for a callback that will be invoked when the entries should be expired.
        /// IsExpired MUST be set before the callback is invoked.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        IDisposable RegisterExpirationCallback(Action<object> callback, object state);
    }
}