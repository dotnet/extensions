// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Internal
{
    /// <summary>
    /// Registers a callback that fires each time a Gen2 garbage collection occurs,
    /// presumably due to memory pressure.
    /// For this to work no components can have a reference to the instance.
    /// </summary>
    public class GcNotification
    {
        private readonly Func<object, bool> _callback;
        private readonly object _state;
        private readonly int _initialCollectionCount;

        private GcNotification(Func<object, bool> callback, object state)
        {
            _callback = callback;
            _state = state;
            _initialCollectionCount = GC.CollectionCount(2);
        }

        public static void Register(Func<object, bool> callback, object state)
        {
            var notification = new GcNotification(callback, state);
        }

        ~GcNotification()
        {
            bool reRegister = true;
            try
            {
                // Only invoke the callback after this instance has made it into gen2.
                if (_initialCollectionCount < GC.CollectionCount(2))
                {
                    reRegister = _callback(_state);
                }
            }
            catch (Exception)
            {
                // Never throw from the finalizer thread
            }

            if (reRegister && !Environment.HasShutdownStarted)
            {
                GC.ReRegisterForFinalize(this);
            }
        }
    }
}