// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Provides an ILoggerFactory based on System.Diagnostics.TraceSource.
    /// </summary>
    public class TraceSourceLoggerProvider : ILoggerProvider
    {
        private readonly SourceSwitch _rootSourceSwitch;
        private readonly TraceListener _rootTraceListener;

        private readonly ConcurrentDictionary<string, TraceSource> _sources = new ConcurrentDictionary<string, TraceSource>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="TraceSourceLoggerProvider"/> class.
        /// </summary>
        /// <param name="rootSourceSwitch"></param>
        /// <param name="rootTraceListener"></param>
        public TraceSourceLoggerProvider([NotNull]SourceSwitch rootSourceSwitch, [NotNull]TraceListener rootTraceListener)
        {
            _rootSourceSwitch = rootSourceSwitch;
            _rootTraceListener = rootTraceListener;
        }

        /// <summary>
        /// Creates a new <see cref="ILogger"/>  for the given component name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ILogger CreateLogger(string name)
        {
            return new TraceSourceLogger(GetOrAddTraceSource(name));
        }

        private TraceSource GetOrAddTraceSource(string name)
        {
            return _sources.GetOrAdd(name, InitializeTraceSource);
        }

        private TraceSource InitializeTraceSource(string traceSourceName)
        {
            var traceSource = new TraceSource(traceSourceName);
            string parentSourceName = ParentSourceName(traceSourceName);

            if (string.IsNullOrEmpty(parentSourceName))
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
            return indexOfLastDot == -1 ? null : traceSourceName.Substring(0, indexOfLastDot);
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
