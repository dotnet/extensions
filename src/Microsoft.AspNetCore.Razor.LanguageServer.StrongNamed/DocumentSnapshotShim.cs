// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    public abstract class DocumentSnapshotShim
    {
        public abstract HostDocumentShim HostDocument { get; }

        public abstract string FilePath { get; }

        public abstract string TargetPath { get; }

        public abstract IReadOnlyList<DocumentSnapshotShim> GetImports();

        public abstract Task<SourceText> GetTextAsync();

        public abstract Task<VersionStamp> GetTextVersionAsync();

        public abstract Task<RazorCodeDocument> GetGeneratedOutputAsync();

        public abstract bool TryGetText(out SourceText result);

        public abstract bool TryGetTextVersion(out VersionStamp result);

        public abstract bool TryGetGeneratedOutput(out RazorCodeDocument result);
    }
}
