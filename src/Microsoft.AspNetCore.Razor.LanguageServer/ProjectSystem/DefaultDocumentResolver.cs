// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
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
            if (!_projectResolver.TryResolvePotentialProject(normalizedPath, out var project))
            {
                project = _projectResolver.GetMiscellaneousProject();
            }

            if (!project.DocumentFilePaths.Contains(normalizedPath, FilePathComparer.Instance))
            {
                // Miscellaneous project and other tracked projects do not contain document.
                document = null;
                return false;
            }

            document = project.GetDocument(normalizedPath);
            return true;
        }
    }
}
