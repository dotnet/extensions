// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    internal class DefaultDocumentSnapshotShim : DocumentSnapshotShim
    {
        private readonly DocumentSnapshot _snapshot;

        public DefaultDocumentSnapshotShim(DocumentSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public override HostDocumentShim HostDocument
        {
            get
            {
                if (_snapshot is DefaultDocumentSnapshot defaultSnapshot)
                {
                    return new DefaultHostDocumentShim(defaultSnapshot.State.HostDocument);
                }

                return null;
            }
        }

        public override string FilePath => _snapshot.FilePath;

        public override string TargetPath => _snapshot.TargetPath;

        public override Task<RazorCodeDocument> GetGeneratedOutputAsync() => _snapshot.GetGeneratedOutputAsync();

        public override IReadOnlyList<DocumentSnapshotShim> GetImports()
        {
            var imports = new List<DocumentSnapshotShim>();
            var innerImports = _snapshot.GetImports();

            for (var i = 0; i < innerImports.Count; i++)
            {
                imports.Add(new DefaultDocumentSnapshotShim(innerImports[i]));
            }

            return imports;
        }

        public override Task<SourceText> GetTextAsync() => _snapshot.GetTextAsync();

        public override Task<VersionStamp> GetTextVersionAsync() => _snapshot.GetTextVersionAsync();

        public override bool TryGetGeneratedOutput(out RazorCodeDocument result) => _snapshot.TryGetGeneratedOutput(out result);

        public override bool TryGetText(out SourceText result) => _snapshot.TryGetText(out result);

        public override bool TryGetTextVersion(out VersionStamp result) => _snapshot.TryGetTextVersion(out result);
    }
}
