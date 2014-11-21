// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Ninject.Activation;
using Ninject.Infrastructure.Disposal;
using Ninject.Parameters;
using Ninject.Planning.Targets;

namespace Microsoft.Framework.DependencyInjection.Ninject
{
    internal class KScopeParameter : IParameter, IDisposable, IDisposableObject, INotifyWhenDisposed
    {
        public string Name
        {
            get { return typeof(KScopeParameter).FullName; }
        }

        public bool ShouldInherit
        {
            get { return true; }
        }

        public object GetValue(IContext context, ITarget target)
        {
            return null;
        }

        public bool Equals(IParameter other)
        {
            return this == other;
        }

        public void Dispose()
        {
            var disposed = Disposed;
            if (disposed != null)
            {
                disposed(this, EventArgs.Empty);
            }

            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }

        public event EventHandler Disposed;
    }
}
