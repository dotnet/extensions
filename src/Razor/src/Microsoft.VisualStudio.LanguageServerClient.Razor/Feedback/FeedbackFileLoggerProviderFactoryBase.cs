// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    [Shared]
    internal abstract class FeedbackFileLoggerProviderFactoryBase : FeedbackFileLoggerProviderFactory
    {
        private readonly object _creationLock;
        private readonly FeedbackLogDirectoryProvider _feedbackLogDirectoryProvider;
        private DefaultFeedbackFileLogWriter _currentFileLogWriter;

        public FeedbackFileLoggerProviderFactoryBase(FeedbackLogDirectoryProvider feedbackLogDirectoryProvider)
        {
            if (feedbackLogDirectoryProvider is null)
            {
                throw new ArgumentNullException(nameof(feedbackLogDirectoryProvider));
            }

            _feedbackLogDirectoryProvider = feedbackLogDirectoryProvider;
            _creationLock = new object();
        }

        public override object GetOrCreate(string logFileIdentifier)
        {
            lock (_creationLock)
            {
                if (_currentFileLogWriter != null)
                {
                    // Dispose last log writer so we can start a new session. Technically only one should only ever be active at a time.
                    _currentFileLogWriter.Dispose();
                }

                _currentFileLogWriter = new DefaultFeedbackFileLogWriter(_feedbackLogDirectoryProvider, logFileIdentifier);
                var provider = new FeedbackFileLoggerProvider(_currentFileLogWriter);

                return provider;
            }
        }
    }
}
