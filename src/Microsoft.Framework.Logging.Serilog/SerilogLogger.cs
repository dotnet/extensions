// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Serilog.Events;
using Serilog.Core;

using SLogger = Serilog.ILogger;

namespace Microsoft.Framework.Logging.Serilog
{
    public class SerilogLogger : ILogger
    {
        private readonly SerilogLoggerProvider _provider;
        private readonly string _name;
        private readonly SLogger _logger;

        public SerilogLogger(
            [NotNull] SerilogLoggerProvider provider,
            [NotNull] SLogger logger,
            string name)
        {
            _provider = provider;
            _name = name;
            _logger = logger.ForContext(Constants.SourceContextPropertyName, name);
        }

        public IDisposable BeginScope(object state)
        {
            return _provider.BeginScope(_name, state);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(ConvertLevel(logLevel));
        }

        public void Write(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            var level = ConvertLevel(logLevel);
            if (!_logger.IsEnabled(level))
            {
                return;
            }

            var logger = _logger;

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
            var structure = state as ILoggerStructure;
            if (structure != null)
            {
                logger = logger.ForContext(new[] { new StructureEnricher(structure) });
            }
            if (exception != null)
            {
                logger = logger.ForContext(new[] { new ExceptionEnricher(exception) });
            }

            logger.Write(level, "{Message:l}", message);
        }

        private LogEventLevel ConvertLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return LogEventLevel.Fatal;
                case LogLevel.Error:
                    return LogEventLevel.Error;
                case LogLevel.Warning:
                    return LogEventLevel.Warning;
                case LogLevel.Information:
                    return LogEventLevel.Information;
                case LogLevel.Verbose:
                    return LogEventLevel.Verbose;
                default:
                    throw new NotSupportedException();
            }
        }

        private class StructureEnricher : ILogEventEnricher
        {
            private readonly ILoggerStructure _structure;

            public StructureEnricher(ILoggerStructure structure)
            {
                _structure = structure;
            }

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                foreach (var value in _structure.GetValues())
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                        value.Key,
                        value.Value));
                }
            }
        }

        private sealed class ExceptionEnricher : ILogEventEnricher
        {
            private readonly Exception _exception;

            public ExceptionEnricher(Exception exception)
            {
                _exception = exception;
            }

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "Exception",
                    _exception.ToString()));

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ExceptionType",
                    _exception.GetType().FullName));

                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ExceptionMessage",
                    _exception.Message));
            }
        }
    }
}