// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Editor.Razor.Documents;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    [Shared]
    [ExportWorkspaceServiceFactory(typeof(EditorDocumentManager), ServiceLayer.Host)]
    internal class VisualStudioMacEditorDocumentManagerFactory : IWorkspaceServiceFactory
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;

        [ImportingConstructor]
        public VisualStudioMacEditorDocumentManagerFactory(ForegroundDispatcher foregroundDispatcher)
        {
            if (foregroundDispatcher is null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            _foregroundDispatcher = foregroundDispatcher;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            var fileChangeTrackerFactory = workspaceServices.GetRequiredService<FileChangeTrackerFactory>();
            var editorDocumentManager = new VisualStudioMacEditorDocumentManager(_foregroundDispatcher, fileChangeTrackerFactory);
            return editorDocumentManager;
        }
    }
}
