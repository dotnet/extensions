// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [FileExtension(RazorLSPConstants.RazorFileExtension)]
    [Name(nameof(RazorFilePathToContentTypeProvider))]
    [Export(typeof(IFilePathToContentTypeProvider))]
    internal class RazorFilePathToContentTypeProvider : RazorFilePathToContentTypeProviderBase
    {
        [ImportingConstructor]
        public RazorFilePathToContentTypeProvider(
            IContentTypeRegistryService contentTypeRegistryService,
            LSPEditorFeatureDetector lspEditorFeatureDetector) : base(contentTypeRegistryService, lspEditorFeatureDetector)
        {
        }
    }
}
