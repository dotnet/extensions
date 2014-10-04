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

        public void Write(TraceType traceType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(traceType))
            {
                return;
            }
            var message = string.Empty;
            if (formatter != null)
            {
                message = formatter(state, exception);
            }
            else
            {
                if (state != null)
                {
                    message += state;
                }
                if (exception != null)
                {
                    message += Environment.NewLine + exception;
                }
            }
            if (!string.IsNullOrEmpty(message))
            {
                _traceSource.TraceEvent(GetEventType(traceType), eventId, message);
            }
        }

        public bool IsEnabled(TraceType traceType)
        {
            var eventType = GetEventType(traceType);
            return _traceSource.Switch.ShouldTrace(eventType);
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
