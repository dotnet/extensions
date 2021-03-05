// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.Extensions.Logging;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    [Shared]
    [Export(typeof(LegacyHTMLCSharpLanguageServerFeedbackFileLoggerProvider))]
    [Obsolete("Use the LogHub logging infrastructure instead.")]
    internal class LegacyHTMLCSharpLanguageServerFeedbackFileLoggerProvider : ILoggerProvider
    {
        private static readonly string LogFileIdentifier = "HTMLCSharpLanguageServer";

        private readonly FeedbackFileLoggerProvider _loggerProvider;

        // Internal for testing
        internal LegacyHTMLCSharpLanguageServerFeedbackFileLoggerProvider()
        {
        }

        [ImportingConstructor]
        public LegacyHTMLCSharpLanguageServerFeedbackFileLoggerProvider(
            HTMLCSharpLanguageServerFeedbackFileLoggerProviderFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _loggerProvider = (FeedbackFileLoggerProvider)loggerFactory.GetOrCreate(LogFileIdentifier);
        }

        // Virtual for testing
        public virtual ILogger CreateLogger(string categoryName) => _loggerProvider.CreateLogger(categoryName);

        public void Dispose()
        {
        }
    }
}
