// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public abstract class VirtualDocumentSnapshot
    {
        public abstract Uri Uri { get; }

        public abstract ITextSnapshot Snapshot { get; }

        public abstract long? HostDocumentSyncVersion { get; }
    }
}
