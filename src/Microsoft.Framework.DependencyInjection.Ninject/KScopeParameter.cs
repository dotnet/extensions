// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using Ninject.Activation;
using Ninject.Infrastructure.Disposal;
using Ninject.Parameters;
using Ninject.Planning.Targets;

namespace Microsoft.Framework.DependencyInjection.Ninject
{
    internal class KScopeParameter : IParameter, IDisposable, IDisposableObject, INotifyWhenDisposed
    {
        public KScopeParameter(IServiceProvider fallbackProvder)
        {
            FallbackProvider = fallbackProvder;
        }

        public string Name
        {
            get { return typeof(KScopeParameter).FullName; }
        }

        public bool ShouldInherit
        {
            get { return true; }
        }

        public IServiceProvider FallbackProvider { get; private set; }

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
