// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Provides an ILoggerFactory based on System.Diagnostics.TraceSource.
    /// </summary>
    public class DiagnosticsLoggerFactory : ILoggerFactory
    {
        private const string RootTraceName = "Microsoft.AspNet";

        private readonly SourceSwitch _rootSourceSwitch;
        private readonly TraceListener _rootTraceListener;

        private readonly ConcurrentDictionary<string, TraceSource> _sources = new ConcurrentDictionary<string, TraceSource>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsLoggerFactory"/> class. 
        /// </summary>
        /// <summary>
        /// Creates a factory named "Microsoft.Owin".
        /// </summary>
        public DiagnosticsLoggerFactory()
        {
            _rootSourceSwitch = new SourceSwitch(RootTraceName);
            _rootTraceListener = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticsLoggerFactory"/> class.
        /// </summary>
        /// <param name="rootSourceSwitch"></param>
        /// <param name="rootTraceListener"></param>
        public DiagnosticsLoggerFactory(SourceSwitch rootSourceSwitch, TraceListener rootTraceListener)
        {
            _rootSourceSwitch = rootSourceSwitch ?? new SourceSwitch(RootTraceName);
            _rootTraceListener = rootTraceListener;
        }

        /// <summary>
        /// Creates a new DiagnosticsLogger for the given component name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ILogger Create(string name)
        {
            return new DiagnosticsLogger(GetOrAddTraceSource(name));
        }

        private TraceSource GetOrAddTraceSource(string name)
        {
            return _sources.GetOrAdd(name, InitializeTraceSource);
        }

        private TraceSource InitializeTraceSource(string traceSourceName)
        {
            var traceSource = new TraceSource(traceSourceName);
            if (traceSourceName == RootTraceName)
            {
                if (HasDefaultSwitch(traceSource))
                {
                    traceSource.Switch = _rootSourceSwitch;
                }
                if (_rootTraceListener != null)
                {
                    traceSource.Listeners.Add(_rootTraceListener);
                }
            }
            else
            {
                string parentSourceName = ParentSourceName(traceSourceName);
                if (HasDefaultListeners(traceSource))
                {
                    TraceSource parentTraceSource = GetOrAddTraceSource(parentSourceName);
                    traceSource.Listeners.Clear();
                    traceSource.Listeners.AddRange(parentTraceSource.Listeners);
                }
                if (HasDefaultSwitch(traceSource))
                {
                    TraceSource parentTraceSource = GetOrAddTraceSource(parentSourceName);
                    traceSource.Switch = parentTraceSource.Switch;
                }
            }

            return traceSource;
        }

        private static string ParentSourceName(string traceSourceName)
        {
            int indexOfLastDot = traceSourceName.LastIndexOf('.');
            return indexOfLastDot == -1 ? RootTraceName : traceSourceName.Substring(0, indexOfLastDot);
        }

        private static bool HasDefaultListeners(TraceSource traceSource)
        {
            return traceSource.Listeners.Count == 1 && traceSource.Listeners[0] is DefaultTraceListener;
        }

        private static bool HasDefaultSwitch(TraceSource traceSource)
        {
            return string.IsNullOrEmpty(traceSource.Switch.DisplayName) == string.IsNullOrEmpty(traceSource.Name) &&
                traceSource.Switch.Level == SourceLevels.Off;
        }
    }
}
#endif