// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Wraps a JS interop argument, indicating that the value should not be serialized as JSON
    /// but instead should be passed as a reference.
    ///
    /// To avoid leaking memory, the reference must later be disposed by JS code or by .NET code.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to wrap.</typeparam>
    public sealed class DotNetObjectReference<TValue> : IDotNetObjectReference, IDisposable where TValue : class
    {
        private readonly TValue _value;
        private long _objectId;
        private JSRuntime _jsRuntime;

        /// <summary>
        /// Initializes a new instance of <see cref="DotNetObjectReference{TValue}" />.
        /// </summary>
        /// <param name="value">The value to pass by reference.</param>
        internal DotNetObjectReference(TValue value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the object instance represented by this wrapper.
        /// </summary>
        public TValue Value
        {
            get
            {
                ThrowIfDisposed();
                return _value;
            }
        }

        internal long ObjectId
        {
            get
            {
                ThrowIfDisposed();
                Debug.Assert(_objectId != 0, "Accessing ObjectId without tracking is always incorrect.");

                return _objectId;
            }
        }

        internal long TrackObjectReference(JSRuntime jsRuntime)
        {
            if (jsRuntime == null)
            {
                throw new ArgumentNullException(nameof(jsRuntime));
            }

            ThrowIfDisposed();

            if (_jsRuntime is null)
            {
                _jsRuntime = jsRuntime;
                _objectId = jsRuntime.TrackObjectReference(this);
            }
            else if (!ReferenceEquals(_jsRuntime, jsRuntime))
            {
                throw new InvalidOperationException($"{GetType().Name} is already being tracked by a different instance of {nameof(JSRuntime)}. A common cause is caching an instance of {nameof(DotNetObjectReference<TValue>)}" +
                    $" globally. Consider creating instances of {nameof(DotNetObjectReference<TValue>)} at the JSInterop callsite instead");
            }

            Debug.Assert(_objectId != 0, "Object must already be tracked");
            return _objectId;
        }

        object IDotNetObjectReference.Value => Value;

        internal bool Disposed { get; private set; }

        /// <summary>
        /// Stops tracking this object reference, allowing it to be garbage collected
        /// (if there are no other references to it). Once the instance is disposed, it
        /// can no longer be used in interop calls from JavaScript code.
        /// </summary>
        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;

                if (_jsRuntime != null)
                {
                    _jsRuntime.ReleaseObjectReference(_objectId);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
