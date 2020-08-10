// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    internal class FeedbackFileLoggerProvider : ILoggerProvider
    {
        // Internal for testing
        internal const string OmniSharpFrameworkCategoryPrefix = "OmniSharp.Extensions.LanguageServer.Server";
        private const string RazorLanguageServerPrefix = "Microsoft.AspNetCore.Razor.LanguageServer";
        private readonly FeedbackFileLogWriter _feedbackFileLogWriter;

        public FeedbackFileLoggerProvider(FeedbackFileLogWriter feedbackFileLogWriter)
        {
            if (feedbackFileLogWriter is null)
            {
                throw new ArgumentNullException(nameof(feedbackFileLogWriter));
            }

            _feedbackFileLogWriter = feedbackFileLogWriter;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (categoryName is null)
            {
                throw new ArgumentNullException(nameof(categoryName));
            }

            if (categoryName.StartsWith(OmniSharpFrameworkCategoryPrefix, StringComparison.Ordinal))
            {
                // Loggers created for O# framework pieces should be ignored for feedback. They emit too much noise.
                return NoopLogger.Instance;
            }

            if (categoryName.StartsWith(RazorLanguageServerPrefix, StringComparison.Ordinal))
            {
                // Reduce the size of the Razor language server categories. We assume nearly all logs here are going to be from the server and thus limiting
                // the amount of noise they emit will be valuable for readability and will ultimately reduce the size of the log on the users box.
                categoryName = categoryName.Substring(RazorLanguageServerPrefix.Length + 1 /* . */);
            }

            return new FeedbackFileLogger(categoryName, _feedbackFileLogWriter);
        }

        public void Dispose()
        {
        }

        private class NoopLogger : ILogger
        {
            public static readonly ILogger Instance = new NoopLogger();

            private NoopLogger()
            {
            }

            public IDisposable BeginScope<TState>(TState state) => Scope.Instance;

            public bool IsEnabled(LogLevel logLevel) => false;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
            }

            private class Scope : IDisposable
            {
                public static readonly Scope Instance = new Scope();

                public void Dispose()
                {
                }
            }
        }
    }
}
