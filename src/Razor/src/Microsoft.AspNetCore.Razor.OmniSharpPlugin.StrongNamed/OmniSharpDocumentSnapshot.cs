// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public sealed class OmniSharpDocumentSnapshot
    {
        private readonly DocumentSnapshot _documentSnapshot;
        private OmniSharpHostDocument _hostDocument;

        internal OmniSharpDocumentSnapshot(DocumentSnapshot documentSnapshot)
        {
            if (documentSnapshot == null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            _documentSnapshot = documentSnapshot;
        }

        public OmniSharpHostDocument HostDocument
        {
            get
            {
                if (_hostDocument == null)
                {
                    var defaultDocumentSnapshot = (DefaultDocumentSnapshot)_documentSnapshot;
                    var hostDocument = defaultDocumentSnapshot.State.HostDocument;
                    _hostDocument = new OmniSharpHostDocument(hostDocument.FilePath, hostDocument.TargetPath, hostDocument.FileKind);
                }

                return _hostDocument;
            }
        }

        public string FileKind => _documentSnapshot.FileKind;

        public string FilePath => _documentSnapshot.FilePath;

        public string TargetPath => _documentSnapshot.TargetPath;
    }
}
