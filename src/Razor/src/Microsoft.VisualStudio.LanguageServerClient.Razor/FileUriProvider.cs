// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    internal abstract class FileUriProvider
    {
        public abstract Uri GetOrCreate(ITextBuffer textBuffer);
    }
}
