// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Export(typeof(VSLanguageServerFeatureOptions))]
    internal class VSLanguageServerFeatureOptions : LanguageServerFeatureOptions
    {
        private readonly LSPEditorFeatureDetector _lspEditorFeatureDetector;

        [ImportingConstructor]
        public VSLanguageServerFeatureOptions(LSPEditorFeatureDetector lspEditorFeatureDetector)
        {
            if (lspEditorFeatureDetector is null)
            {
                throw new ArgumentNullException(nameof(lspEditorFeatureDetector));
            }

            _lspEditorFeatureDetector = lspEditorFeatureDetector;
        }

        // We don't currently support file creation operations on VS Codespaces or VS Liveshare
        public override bool SupportsFileManipulation => !IsCodespacesOrLiveshare;

        private bool IsCodespacesOrLiveshare => _lspEditorFeatureDetector.IsRemoteClient() || _lspEditorFeatureDetector.IsLiveShareHost();
    }
}
