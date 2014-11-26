// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.Framework.Cache.Memory.Infrastructure
{
    internal class TestTrigger : IExpirationTrigger
    {
        private bool _isExpired;
        private bool _activeExpirationCallbacks;

        public bool IsExpired
        {
            get
            {
                IsExpiredWasCalled = true;
                return _isExpired;
            }
            set
            {
                _isExpired = value;
            }
        }

        public bool IsExpiredWasCalled { get; set; }

        public bool ActiveExpirationCallbacks
        {
            get
            {
                ActiveExpirationCallbacksWasCalled = true;
                return _activeExpirationCallbacks;
            }
            set
            {
                _activeExpirationCallbacks = value;
            }
        }

        public bool ActiveExpirationCallbacksWasCalled { get; set; }

        public TriggerCallbackRegistration Registration { get; set; }

        public IDisposable RegisterExpirationCallback(Action<object> callback, object state)
        {
            Registration = new TriggerCallbackRegistration()
            {
                RegisteredCallback = callback,
                RegisteredState = state,
            };
            return Registration;
        }

        public void Fire()
        {
            IsExpired = true;
            if (Registration != null && !Registration.Disposed)
            {
                Registration.RegisteredCallback(Registration.RegisteredState);
            }
        }
    }
}