// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Logging;

namespace Microsoft.Framework.Logging.Console
{
    public class ConsoleLogger : ILogger
    {
        private readonly string _name;
        private Func<string, TraceType, bool> _filter;

        public ConsoleLogger(string name, Func<string, TraceType, bool> filter)
        {
            _name = name;
            _filter = filter ?? ((category, traceType) => true);
            Console = new LogConsole();
        }

        public IConsole Console { get; set; }

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
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            var foregroundColor = Console.ForegroundColor;  // save current colors
            var backgroundColor = Console.BackgroundColor;
            var severity = traceType.ToString().ToUpperInvariant();
            SetConsoleColor(traceType);
            try
            {
                Console.WriteLine("[{0}:{1}] {2}", severity, _name, message);
            }
            finally
            {
                Console.ForegroundColor = foregroundColor;  // reset initial colors
                Console.BackgroundColor = backgroundColor;
            }
        }

        public bool IsEnabled(TraceType traceType)
        {
            return _filter(_name, traceType);
        }

        // sets the console text color to reflect the given TraceType
        private void SetConsoleColor(TraceType traceType)
        {
            switch (traceType)
            {
                case TraceType.Critical:
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case TraceType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case TraceType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case TraceType.Information:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case TraceType.Verbose:
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }
        }

        public IDisposable BeginScope(object state)
        {
            return null;
        }
    }
}