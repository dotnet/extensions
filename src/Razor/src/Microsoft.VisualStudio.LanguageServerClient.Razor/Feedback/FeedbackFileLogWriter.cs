// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback
{
    internal abstract class FeedbackFileLogWriter
    {
        public abstract void Write(string message);
    }
}
