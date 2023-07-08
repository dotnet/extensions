// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This source file was lovingly 'borrowed' from dotnet/runtime/src/libraries/Microsoft.Extensions.Logging
#pragma warning disable S1128 // Unused "using" should be removed
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1128 // Put constructor initializers on their own line
#pragma warning disable SA1127 // Generic type constraints should be on their own line
#pragma warning disable CS8602 // Dereference of a possibly null reference.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Telemetry.Logging;

namespace Microsoft.Extensions.Logging
{
    internal readonly struct MessageLogger
    {
        public MessageLogger(ILogger logger, string? category, string? providerTypeFullName, LogLevel? minLevel, Func<string?, string?, LogLevel, bool>? filter)
        {
            Logger = logger;
            Category = category;
            ProviderTypeFullName = providerTypeFullName;
            MinLevel = minLevel;
            Filter = filter;

            // perform the GVM lookup once, rather than on every call.
            LoggerIsEnabled = logger.IsEnabled;
            LoggerLog = logger.Log<ExtendedLogger.PropertyJoiner>;
        }

        public Func<LogLevel, bool> LoggerIsEnabled { get; }

        public Action<LogLevel, EventId, ExtendedLogger.PropertyJoiner, Exception?, Func<ExtendedLogger.PropertyJoiner, Exception?, string>> LoggerLog { get; }

        public ILogger Logger { get; }

        public string? Category { get; }

        private string? ProviderTypeFullName { get; }

        public LogLevel? MinLevel { get; }

        public Func<string?, string?, LogLevel, bool>? Filter { get; }

        public bool IsNotFilteredOut(LogLevel level)
        {
            if (MinLevel != null && level < MinLevel)
            {
                return false;
            }

            if (Filter != null)
            {
                return Filter(ProviderTypeFullName, Category, level);
            }

            return true;
        }
    }

    internal readonly struct ScopeLogger
    {
        public ScopeLogger(ILogger? logger, IExternalScopeProvider? externalScopeProvider)
        {
            Logger = logger;
            ExternalScopeProvider = externalScopeProvider;
        }

        public ILogger? Logger { get; }

        public IExternalScopeProvider? ExternalScopeProvider { get; }

        public IDisposable? CreateScope<TState>(TState state) where TState : notnull
        {
            if (ExternalScopeProvider != null)
            {
                return ExternalScopeProvider.Push(state);
            }

            return Logger.BeginScope<TState>(state);
        }
    }

    internal readonly struct LoggerInformation
    {
        public LoggerInformation(ILoggerProvider provider, string category) : this()
        {
            ProviderType = provider.GetType();
            Logger = provider.CreateLogger(category);
            Category = category;
            ExternalScope = provider is ISupportExternalScope;
        }

        public ILogger Logger { get; }

        public string Category { get; }

        public Type ProviderType { get; }

        public bool ExternalScope { get; }
    }
}
