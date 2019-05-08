// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Wraps a JS interop argument, indicating that the value should not be serialized as JSON
    /// but instead should be passed as a reference.
    ///
    /// To avoid leaking memory, the reference must later be disposed by JS code or by .NET code.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to wrap.</typeparam>
    public sealed class DotNetObjectRef<TValue> : IDotNetObjectRef, IDisposable where TValue : class
    {
        private long? _trackingId;

        /// <summary>
        /// This A is for meant for JSON deserialization and should not be used by user code.
        /// </summary>
        [Obsolete("This API is meant for JSInterop infrastructure and should not be used by user code.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DotNetObjectRef()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="DotNetObjectRef{TValue}" />.
        /// </summary>
        /// <param name="value">The value to pass by reference.</param>
        public DotNetObjectRef(TValue value)
        {
            Value = value;
            _trackingId = DotNetObjectRefManager.Current.TrackObject(this);
        }

        /// <summary>
        /// Gets the object instance represented by this wrapper.
        /// </summary>
        [JsonIgnore]
        public TValue Value
        {
            get;
            // Workaround for https://github.com/dotnet/corefx/issues/37536 and https://github.com/dotnet/corefx/issues/37567
            // Once fixed, we should make this property private.
            set;
        }

        internal bool Disposed { get; private set; }

        /// <summary>
        /// This constructor is for meant for JSON serialization and should not be used by user code.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [JsonPropertyName(DotNetDispatcher.DotNetObjectRefKey)]
        [Obsolete("This API is meant for JSInterop infrastructure and should not be used by user code.")]
        public long TrackingId
        {
            get
            {
                ThrowIfDisposed();
                return _trackingId.Value;
            }
            set
            {
                ThrowIfDisposed();
                if (_trackingId != null)
                {
                    throw new InvalidOperationException($"{nameof(DotNetObjectRef<TValue>)} cannot be reinitialized.");
                }

                _trackingId = value;
                Value = (TValue)DotNetObjectRefManager.Current.FindDotNetObject(value);
            }
        }

        object IDotNetObjectRef.Value => Value;

        /// <summary>
        /// Implictly converts the <see cref="DotNetObjectRef{TValue}" /> instance to the wrapping value.
        /// </summary>
        /// <param name="result">The <see cref="DotNetObjectRef{TValue}"/>.</param>
        public static implicit operator TValue(DotNetObjectRef<TValue> result) => result.Value;

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
                // Make sure the object was constructed correctly.
                DotNetObjectRefManager.Current.ReleaseDotNetObject(_trackingId.Value);
            }
        }

        private void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(DotNetObjectRef<TValue>));
            }
        }

        // Used by the RefManager to mark this instance as disposed once the object is no longer being tracked.
        void IDotNetObjectRef.SetDisposed()
        {
            Disposed = true;
        }
    }
}
