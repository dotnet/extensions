// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Logging;
using Moq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public abstract class HandlerTestBase
    {
        public HandlerTestBase()
        {
            var logger = new TestLogger();
            LoggerProvider = Mock.Of<HTMLCSharpLanguageServerLogHubLoggerProvider>(l =>
                l.CreateLogger(It.IsAny<string>()) == logger && l.InitializeLoggerAsync(It.IsAny<CancellationToken>()) == Task.CompletedTask,
                MockBehavior.Strict);
        }

        internal HTMLCSharpLanguageServerLogHubLoggerProvider LoggerProvider { get; }

        private class TestLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
            {
                return default;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                // noop
            }
        }
    }
}
