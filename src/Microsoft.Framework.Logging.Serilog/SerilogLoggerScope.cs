// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if ASPNET50
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#endif

namespace Microsoft.Framework.Logging.Serilog
{
    public class SerilogLoggerScope : IDisposable
    {
        private readonly SerilogLoggerProvider _provider;

        public SerilogLoggerScope(SerilogLoggerProvider provider, SerilogLoggerScope parent, string name, object state)
        {
            _provider = provider;
            Name = name;
            State = state;

            Parent = _provider.CurrentScope;
            _provider.CurrentScope = this;
        }

        public SerilogLoggerScope Parent { get; private set; }
        public string Name { get; private set; }
        public object State { get; private set; }

        public void RemoveScope()
        {
            for (var scan = _provider.CurrentScope; scan != null; scan = scan.Parent)
            {
                if (ReferenceEquals(scan, this))
                {
                    _provider.CurrentScope = Parent;
                }
            }
        }

        private bool _disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            if (!_disposedValue)
            {
                RemoveScope();
            }
            _disposedValue = true;
        }
    }
}