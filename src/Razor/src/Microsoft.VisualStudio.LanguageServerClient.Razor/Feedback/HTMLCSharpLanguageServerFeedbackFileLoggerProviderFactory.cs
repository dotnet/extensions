// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Composition;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    [Shared]
    [Export(typeof(HTMLCSharpLanguageServerFeedbackFileLoggerProviderFactory))]
    internal class HTMLCSharpLanguageServerFeedbackFileLoggerProviderFactory : FeedbackFileLoggerProviderFactoryBase
    {
        [ImportingConstructor]
        public HTMLCSharpLanguageServerFeedbackFileLoggerProviderFactory(FeedbackLogDirectoryProvider feedbackLogDirectoryProvider)
            : base(feedbackLogDirectoryProvider)
        {
        }
    }
}
