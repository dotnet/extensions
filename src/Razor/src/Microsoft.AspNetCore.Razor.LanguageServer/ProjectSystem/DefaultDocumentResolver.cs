// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class DefaultDocumentResolver : DocumentResolver
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly ProjectResolver _projectResolver;
        private readonly FilePathNormalizer _filePathNormalizer;

        public DefaultDocumentResolver(
            ForegroundDispatcher foregroundDispatcher,
            ProjectResolver projectResolver,
            FilePathNormalizer filePathNormalizer)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectResolver == null)
            {
                throw new ArgumentNullException(nameof(projectResolver));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectResolver = projectResolver;
            _filePathNormalizer = filePathNormalizer;
        }

        public override bool TryResolveDocument(string documentFilePath, out DocumentSnapshot document)
        {
            _foregroundDispatcher.AssertForegroundThread();

            var normalizedPath = _filePathNormalizer.Normalize(documentFilePath);
            if (!_projectResolver.TryResolveProject(normalizedPath, out var project))
            {
                // Neither the potential project determined by file path,
                // nor the Miscellaneous project contain the document.
                document = null;
                return false;
            }

            document = project.GetDocument(normalizedPath);
            return true;
        }
    }
}
