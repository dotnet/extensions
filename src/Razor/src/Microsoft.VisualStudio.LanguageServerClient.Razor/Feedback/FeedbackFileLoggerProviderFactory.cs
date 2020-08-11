// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    internal abstract class FeedbackFileLoggerProviderFactory
    {
        /// <summary>
        /// Returns a <see cref="FeedbackFileLoggerProvider"/>. This is an optimization to ensure we don't load an extra logging dll at MEF load time in Visual Studio.
        /// </summary>
        /// <returns>A created <see cref="FeedbackFileLoggerProvider"/>.</returns>
        public abstract object GetOrCreate();
    }
}
