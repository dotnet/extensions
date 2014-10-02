// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45 || ASPNET50 || ASPNETCORE50
using System;
using System.Diagnostics;

namespace Microsoft.Framework.Logging
{
    internal class DiagnosticsLogger : ILogger
    {
        private readonly TraceSource _traceSource;

        public DiagnosticsLogger(TraceSource traceSource)
        {
            _traceSource = traceSource;
        }

        public bool WriteCore(TraceType traceType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            var eventType = GetEventType(traceType);

            if (!_traceSource.Switch.ShouldTrace(eventType))
            {
                return false;
            }
            else if (formatter != null)
            {
                _traceSource.TraceEvent(eventType, eventId, formatter(state, exception));
            }
            return true;
        }

        private static TraceEventType GetEventType(TraceType traceType)
        {
            switch (traceType)
            {
                case TraceType.Critical: return TraceEventType.Critical;
                case TraceType.Error: return TraceEventType.Error;
                case TraceType.Warning: return TraceEventType.Warning;
                case TraceType.Information: return TraceEventType.Information;
                case TraceType.Verbose:
                default: return TraceEventType.Verbose;
            }
        }

        public IDisposable BeginScope(object state)
        {
            return new DiagnosticsScope(state);
        }
    }
}
#endif
