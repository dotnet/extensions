// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class FormattingOptionsProvider
    {
        public abstract FormattingOptions? GetOptions(Uri lspDocumentUri);
    }
}
