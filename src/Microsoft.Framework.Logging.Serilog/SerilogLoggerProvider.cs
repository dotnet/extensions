// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
#if ASPNET50
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif
using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.Framework.Logging.Serilog
{
    public class SerilogLoggerProvider : ILoggerProvider, ILogEventEnricher
    {
        private readonly global::Serilog.ILogger _logger;

        public SerilogLoggerProvider([NotNull] global::Serilog.LoggerConfiguration loggerConfiguration)
        {
            _logger = loggerConfiguration
                .Enrich.With(this)
                .CreateLogger();
        }

        public ILogger Create(string name)
        {
            return new SerilogLogger(this, _logger, name);
        }

        public IDisposable BeginScope(string name, object state)
        {
            return new SerilogLoggerScope(this, CurrentScope, name, state);
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            for (var scope = CurrentScope; scope != null; scope = scope.Parent)
            {
                var stateStructure = scope.State as ILoggerStructure;
                if (stateStructure != null)
                {
                    foreach (var keyValue in stateStructure.GetValues())
                    {
                        var property = propertyFactory.CreateProperty(keyValue.Key, keyValue.Value);
                        logEvent.AddPropertyIfAbsent(property);
                    }
                }
            }
        }

#if ASPNETCORE50
        private AsyncLocal<SerilogLoggerScope> _value = new AsyncLocal<SerilogLoggerScope>();
        public SerilogLoggerScope CurrentScope
        {
            get
            {
                return _value.Value;
            }
            set
            {
                _value.Value = value;
            }
        }
#else
        private readonly string _currentScopeKey = nameof(SerilogLoggerScope) + "#" + Guid.NewGuid().ToString("n");

        public SerilogLoggerScope CurrentScope
        {
            get
            {
                var objectHandle = CallContext.LogicalGetData(_currentScopeKey) as ObjectHandle;
                return objectHandle != null ? objectHandle.Unwrap() as SerilogLoggerScope : null;
            }
            set
            {
                CallContext.LogicalSetData(_currentScopeKey, new ObjectHandle(value));
            }
        }
#endif
    }
}