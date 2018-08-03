// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DocumentSnapshotTextLoader : TextLoader
    {
        private readonly DocumentSnapshotShim _documentSnapshot;

        public DocumentSnapshotTextLoader(DocumentSnapshotShim documentSnapshot)
        {
            if (documentSnapshot == null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            _documentSnapshot = documentSnapshot;
        }

        public override async Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            var sourceText = await _documentSnapshot.GetTextAsync();
            var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default);

            return textAndVersion;
        }
    }
}
