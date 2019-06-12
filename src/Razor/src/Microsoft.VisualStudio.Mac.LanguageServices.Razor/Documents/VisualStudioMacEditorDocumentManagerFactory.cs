// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Editor.Razor.Documents
{
    [Shared]
    [Export]
    [ExportWorkspaceServiceFactory(typeof(EditorDocumentManager), ServiceLayer.Host)]
    internal class VisualStudioMacEditorDocumentManagerFactory : IWorkspaceServiceFactory
    {
        [Import]
        public ForegroundDispatcher ForegroundDispatcher { get; set; }

        internal static VisualStudioMacEditorDocumentManager Instance { get; set; }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices == null)
            {
                throw new ArgumentNullException(nameof(workspaceServices));
            }

            if (Instance != null)
            {
                return Instance;
            }

            var fileChangeTrackerFactory = workspaceServices.GetRequiredService<FileChangeTrackerFactory>();
            Instance = new VisualStudioMacEditorDocumentManager(this, fileChangeTrackerFactory);
            return Instance;
        }

        public bool TryGetTextBuffer(string filePath, out ITextBuffer textBuffer)
        {
            textBuffer = null;
            return false;
        }
    }
}
