// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RazorFileSynchronizer : IRazorFileChangeListener
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly RazorProjectService _projectService;

        public RazorFileSynchronizer(
            ForegroundDispatcher foregroundDispatcher,
            RazorProjectService projectService)
        {
            if (foregroundDispatcher is null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (projectService is null)
            {
                throw new ArgumentNullException(nameof(projectService));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _projectService = projectService;
        }

        public void RazorFileChanged(string filePath, RazorFileChangeKind kind)
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _foregroundDispatcher.AssertForegroundThread();

            switch (kind)
            {
                case RazorFileChangeKind.Added:
                    _projectService.AddDocument(filePath);
                    break;
                case RazorFileChangeKind.Removed:
                    _projectService.RemoveDocument(filePath);
                    break;
            }
        }
    }
}
