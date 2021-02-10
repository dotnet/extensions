// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    internal abstract class FeedbackFileLoggerProviderFactory
    {
        /// <summary>
        /// Returns a <see cref="FeedbackFileLoggerProvider"/>. This ensures we don't load an extra logging dll at MEF load time in Visual Studio.
        /// </summary>
        /// <remarks>When VS looks for MEF exports it has to load assembly types that correspond to a contracts signature. SO in our case we used to
        /// return a <see cref="FeedbackFileLoggerProvider"/>; however, that resulted in MEF needing to load the ILoggerProvider (what it implements)
        /// assembly which was Microsoft.Extensions.Logging.Abstractions. This wasn't great because it required MEF to then load that assembly in order
        /// to understand this type. Returning <c>object</c> works around requiring the logging assembly.</remarks>
        /// <param name="logFileIdentifier">An identifier to prefix the log file name with.</param>
        /// <returns>A created <see cref="FeedbackFileLoggerProvider"/>.</returns>
        public abstract object GetOrCreate(string logFileIdentifier);
    }
}
