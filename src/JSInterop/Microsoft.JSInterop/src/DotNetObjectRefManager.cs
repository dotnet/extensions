// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.JSInterop
{
    internal class DotNetObjectRefManager
    {
        private readonly object _storageLock = new object();
        private long _nextId = 1; // Start at 1, because 0 signals "no object"
        private readonly Dictionary<long, IDotNetObjectRef> _trackedRefsById = new Dictionary<long, IDotNetObjectRef>();
        private readonly Dictionary<IDotNetObjectRef, long> _trackedIdsByRef = new Dictionary<IDotNetObjectRef, long>();
        public static DotNetObjectRefManager Current
        {
            get
            {
                if (!(JSRuntime.Current is JSRuntimeBase jsRuntimeBase))
                {
                    throw new InvalidOperationException("JSRuntime must be set up to use DotNetObjectRef.");
                }

                return jsRuntimeBase.ObjectRefManager;
            }
        }

        public long TrackObject(IDotNetObjectRef dotNetObjectRef)
        {
            lock (_storageLock)
            {
                // Assign an ID only if it doesn't already have one
                if (!_trackedIdsByRef.TryGetValue(dotNetObjectRef, out var dotNetObjectId))
                {
                    dotNetObjectId = _nextId++;
                    _trackedRefsById.Add(dotNetObjectId, dotNetObjectRef);
                    _trackedIdsByRef.Add(dotNetObjectRef, dotNetObjectId);
                }

                return dotNetObjectId;
            }
        }

        public object FindDotNetObject(long dotNetObjectId)
        {
            lock (_storageLock)
            {
                return _trackedRefsById.TryGetValue(dotNetObjectId, out var dotNetObjectRef)
                    ? dotNetObjectRef.Value
                    : throw new ArgumentException($"There is no tracked object with id '{dotNetObjectId}'. Perhaps the DotNetObjectRef instance was already disposed.", nameof(dotNetObjectId));
            }
        }

        /// <summary>
        /// Stops tracking the specified .NET object reference.
        /// This overload is typically invoked from JS code via JS interop.
        /// </summary>
        /// <param name="dotNetObjectId">The ID of the <see cref="DotNetObjectRef{TValue}"/>.</param>
        public void ReleaseDotNetObject(long dotNetObjectId)
        {
            lock (_storageLock)
            {
                if (_trackedRefsById.TryGetValue(dotNetObjectId, out var dotNetObjectRef))
                {
                    _trackedRefsById.Remove(dotNetObjectId);
                    _trackedIdsByRef.Remove(dotNetObjectRef);

                    dotNetObjectRef.SetDisposed();
                }
            }
        }
    }
}
