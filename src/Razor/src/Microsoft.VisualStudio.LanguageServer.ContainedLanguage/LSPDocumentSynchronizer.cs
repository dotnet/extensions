// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    internal abstract class LSPDocumentSynchronizer : LSPDocumentManagerChangeTrigger
    {
        public abstract Task<bool> TrySynchronizeVirtualDocumentAsync(int requiredHostDocumentVersion, VirtualDocumentSnapshot virtualDocument, CancellationToken cancellationToken);
    }
}
