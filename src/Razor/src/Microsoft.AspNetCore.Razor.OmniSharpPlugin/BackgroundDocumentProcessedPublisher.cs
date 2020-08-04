// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.OmniSharpPlugin.StrongNamed;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OmniSharp;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    // This class is responsible for listening to document processed events and then synchronizing their existence in the C# workspace.
    // Key scenarios are:
    //   1. A Razor document is not open - Publish Razor documents C# content to workspace under a background convention file path (i.e. Index.razor__bg__virtual.cs)
    //   2. A Razor document is already open - Active C# content for the open doc already exists in the workspace (i.e. Index.razor__virtual.cs), noop.
    //   3. A Razor document gets opened - Remove background generated C# content from workspace so active C# content gets prioritized
    //   4. A Razor document gets closed - Need to transition active C# content to background C# content in the workspace.
    //
    // Since we don't have LSP access to fully understand if a document opens/closes we utilize the convention of our active and background generated C#
    // file paths to understand if a Razor document is open or closed.

    [Shared]
    [Export(typeof(OmniSharpDocumentProcessedListener))]
    internal class BackgroundDocumentProcessedPublisher : OmniSharpDocumentProcessedListener
    {
        // File paths need to align with the file path that's used to create virutal document buffers in the RazorDocumentFactory.ts.
        // The purpose of the alignment is to ensure that when a Razor virtual C# buffer opens we can properly detect its existence.
        internal const string ActiveVirtualDocumentSuffix = "__virtual.cs";
        internal const string BackgroundVirtualDocumentSuffix = "__bg" + ActiveVirtualDocumentSuffix;

        private readonly OmniSharpForegroundDispatcher _foregroundDispatcher;
        private readonly OmniSharpWorkspace _workspace;
        private ILogger _logger;
        private OmniSharpProjectSnapshotManager _projectManager;
        private object _workspaceChangedLock;

        [ImportingConstructor]
        public BackgroundDocumentProcessedPublisher(
            OmniSharpForegroundDispatcher foregroundDispatcher,
            OmniSharpWorkspace workspace,
            ILoggerFactory loggerFactory)
        {
            if (foregroundDispatcher is null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (workspace is null)
            {
                throw new ArgumentNullException(nameof(workspace));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _workspace = workspace;
            _logger = loggerFactory.CreateLogger<BackgroundDocumentProcessedPublisher>();
            _workspaceChangedLock = new object();

            _workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
        }

        // A Razor file has been processed, this portion is responsible for the decision of whether we need to create or update
        // the Razor documents background C# representation.
        public override void DocumentProcessed(OmniSharpDocumentSnapshot document)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _foregroundDispatcher.AssertForegroundThread();

            lock (_workspaceChangedLock)
            {
                if (FileKinds.IsComponentImport(document.FileKind))
                {
                    // Razor component imports don't have any C# to generate anyways, don't do the work. This doesn't capture _ViewImports.cshtml because we never
                    // associated a FileKind with those files.
                    return;
                }

                var openVirtualFilePath = document.FilePath + ActiveVirtualDocumentSuffix;
                var openDocument = _workspace.GetDocument(openVirtualFilePath);
                if (openDocument != null)
                {
                    // This document is open in the editor, no reason for us to populate anything in the workspace the editor will do that.
                    return;
                }

                var backgroundVirtualFilePath = document.FilePath + BackgroundVirtualDocumentSuffix;
                var currentDocument = _workspace.GetDocument(backgroundVirtualFilePath);
                if (currentDocument == null)
                {
                    // Background document doesn't exist, we need to create it

                    var roslynProject = GetRoslynProject(document.Project);
                    if (roslynProject == null)
                    {
                        // There's no Roslyn project associated with the Razor document.
                        _logger.LogTrace($"Could not find a Roslyn project for Razor virtual document '{backgroundVirtualFilePath}'.");
                        return;
                    }

                    var documentId = DocumentId.CreateNewId(roslynProject.Id);
                    var name = Path.GetFileName(backgroundVirtualFilePath);
                    var emptyTextLoader = new EmptyTextLoader(backgroundVirtualFilePath);
                    var documentInfo = DocumentInfo.Create(documentId, name, filePath: backgroundVirtualFilePath, loader: emptyTextLoader);
                    _workspace.AddDocument(documentInfo);
                    currentDocument = _workspace.GetDocument(backgroundVirtualFilePath);

                    Debug.Assert(currentDocument != null, "We just added the document, it should definitely be there.");
                }

                // Update document content

                var sourceText = document.GetGeneratedCodeSourceText();
                _workspace.OnDocumentChanged(currentDocument.Id, sourceText);
            }
        }

        public override void Initialize(OmniSharpProjectSnapshotManager projectManager)
        {
            if (projectManager is null)
            {
                throw new ArgumentNullException(nameof(projectManager));
            }

            _projectManager = projectManager;
            _projectManager.Changed += ProjectManager_Changed;
        }

        // Here we're specifically listening for cases when a user opens a Razor document and an active C# content gets created.
        internal void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            lock (_workspaceChangedLock)
            {
                switch (args.Kind)
                {
                    case WorkspaceChangeKind.DocumentAdded:
                        {
                            // We could technically listen for DocumentAdded here but just because a document gets added doesn't mean it has content included.
                            // Therefore we need to wait for content to populated for Razor files so we don't preemptively remove the corresponding background
                            // C# before the active C# content has been populated.

                            var project = args.NewSolution.GetProject(args.ProjectId);
                            var document = project.GetDocument(args.DocumentId);

                            if (document.FilePath == null)
                            {
                                break;
                            }

                            if (document.FilePath.EndsWith(ActiveVirtualDocumentSuffix, StringComparison.Ordinal) && !document.FilePath.EndsWith(BackgroundVirtualDocumentSuffix, StringComparison.Ordinal))
                            {
                                // Document from editor got opened, clear out any background documents of the same type

                                var razorDocumentFilePath = GetRazorDocumentFilePath(document);
                                var backgroundDocumentFilePath = GetBackgroundVirtualDocumentFilePath(razorDocumentFilePath);
                                var backgroundDocument = GetRoslynDocument(project, backgroundDocumentFilePath);
                                if (backgroundDocument != null)
                                {
                                    _workspace.RemoveDocument(backgroundDocument.Id);
                                }
                            }
                            break;
                        }
                    case WorkspaceChangeKind.DocumentRemoved:
                        {
                            var project = args.OldSolution.GetProject(args.ProjectId);
                            var document = project.GetDocument(args.DocumentId);

                            if (document.FilePath == null)
                            {
                                break;
                            }

                            if (document.FilePath.EndsWith(ActiveVirtualDocumentSuffix, StringComparison.Ordinal) && !document.FilePath.EndsWith(BackgroundVirtualDocumentSuffix, StringComparison.Ordinal))
                            {
                                var razorDocumentFilePath = GetRazorDocumentFilePath(document);

                                if (File.Exists(razorDocumentFilePath))
                                {
                                    // Razor document closed because the backing C# virtual document went away
                                    var backgroundDocumentFilePath = GetBackgroundVirtualDocumentFilePath(razorDocumentFilePath);
                                    var newName = Path.GetFileName(backgroundDocumentFilePath);
                                    var delegatedTextLoader = new DelegatedTextLoader(document);
                                    var movedDocumentInfo = DocumentInfo.Create(args.DocumentId, newName, loader: delegatedTextLoader, filePath: backgroundDocumentFilePath);
                                    _workspace.AddDocument(movedDocumentInfo);
                                }
                            }
                        }
                        break;
                }
            }
        }

        // When the Razor project manager forgets about a document we need remove its background C# representation
        // so that content doesn't get stale.
        private void ProjectManager_Changed(object sender, OmniSharpProjectChangeEventArgs args)
        {
            switch (args.Kind)
            {
                case OmniSharpProjectChangeKind.DocumentRemoved:
                    var roslynProject = GetRoslynProject(args.Older);
                    if (roslynProject == null)
                    {
                        // Project no longer exists
                        return;
                    }

                    var backgroundVirtualFilePath = GetBackgroundVirtualDocumentFilePath(args.DocumentFilePath);
                    var backgroundDocument = GetRoslynDocument(roslynProject, backgroundVirtualFilePath);
                    if (backgroundDocument == null)
                    {
                        // No background document associated
                        return;
                    }

                    // There's still a background document associated with the removed Razor document.
                    _workspace.RemoveDocument(backgroundDocument.Id);
                    break;
            }
        }

        private Project GetRoslynProject(OmniSharpProjectSnapshot project)
        {
            var roslynProject = _workspace.CurrentSolution.Projects.FirstOrDefault(roslynProject => string.Equals(roslynProject.FilePath, project.FilePath, FilePathComparison.Instance));
            return roslynProject;
        }

        private static Document GetRoslynDocument(Project project, string backgroundDocumentFilePath)
        {
            var roslynDocument = project.Documents.FirstOrDefault(document => string.Equals(document.FilePath, backgroundDocumentFilePath, FilePathComparison.Instance));
            return roslynDocument;
        }

        private static string GetRazorDocumentFilePath(Document document)
        {
            if (document.FilePath.EndsWith(BackgroundVirtualDocumentSuffix, StringComparison.Ordinal))
            {
                var razorDocumentFilePath = document.FilePath.Substring(0, document.FilePath.Length - BackgroundVirtualDocumentSuffix.Length);
                return razorDocumentFilePath;
            }
            else if (document.FilePath.EndsWith(ActiveVirtualDocumentSuffix, StringComparison.Ordinal))
            {
                var razorDocumentFilePath = document.FilePath.Substring(0, document.FilePath.Length - ActiveVirtualDocumentSuffix.Length);
                return razorDocumentFilePath;
            }

            Debug.Fail($"The caller should have ensured that '{document.FilePath}' is associated with a Razor file path.");
            return null;
        }

        private static string GetBackgroundVirtualDocumentFilePath(string razorDocumentFilePath)
        {
            var backgroundDocumentFilePath = razorDocumentFilePath + BackgroundVirtualDocumentSuffix;
            return backgroundDocumentFilePath;
        }

        private class DelegatedTextLoader : TextLoader
        {
            private readonly Document _document;

            public DelegatedTextLoader(Document document)
            {
                if (document is null)
                {
                    throw new ArgumentNullException(nameof(document));
                }

                _document = document;
            }

            public async override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                var sourceText = await _document.GetTextAsync();
                var textVersion = await _document.GetTextVersionAsync();
                var textAndVersion = TextAndVersion.Create(sourceText, textVersion);
                return textAndVersion;
            }
        }
    }
}
