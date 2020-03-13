// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.CodeAnalysis.Razor
{
    // This types purpose is to serve as a non-Razor specific document delivery mechanism for Roslyn.
    internal sealed class DefaultDynamicDocumentContainer : DynamicDocumentContainer
    {
        private readonly DocumentSnapshot _documentSnapshot;
        private RazorDocumentExcerptService _excerptService;
        private RazorSpanMappingService _mappingService;

        public DefaultDynamicDocumentContainer(DocumentSnapshot documentSnapshot)
        {
            if (documentSnapshot is null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            _documentSnapshot = documentSnapshot;
        }

        public override string FilePath => _documentSnapshot.FilePath;

        public override TextLoader GetTextLoader(string filePath) => new GeneratedDocumentTextLoader(_documentSnapshot, filePath);

        public override object GetExcerptService()
        {
            if (_excerptService == null)
            {
                var mappingService = (RazorSpanMappingService)GetMappingService();
                _excerptService = new RazorDocumentExcerptService(_documentSnapshot, mappingService);
            }

            return _excerptService;
        }

        public override object GetMappingService()
        {
            if (_mappingService == null)
            {
                _mappingService = new RazorSpanMappingService(_documentSnapshot);
            }

            return _mappingService;
        }
    }
}