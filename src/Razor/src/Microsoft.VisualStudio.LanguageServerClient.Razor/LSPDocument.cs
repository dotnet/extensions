// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public abstract class LSPDocument
    {
        public abstract Uri Uri { get; }

        public abstract IReadOnlyList<VirtualDocument> VirtualDocuments { get; }
    }
}
