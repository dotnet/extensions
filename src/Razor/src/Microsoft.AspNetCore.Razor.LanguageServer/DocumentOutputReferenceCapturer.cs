// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DocumentOutputReferenceCapturer : ProjectSnapshotChangeTrigger
    {
        private readonly Dictionary<string, Task<(RazorCodeDocument, VersionStamp, VersionStamp, VersionStamp)>> _referencedOutputTasks;
        private ProjectSnapshotManagerBase _projectManager;

        public DocumentOutputReferenceCapturer()
        {
            _referencedOutputTasks = new Dictionary<string, Task<(RazorCodeDocument, VersionStamp, VersionStamp, VersionStamp)>>(FilePathComparer.Instance);
        }

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
            _projectManager = projectManager;
            _projectManager.Changed += ProjectManager_Changed;
        }

        private void ProjectManager_Changed(object sender, ProjectChangeEventArgs args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            switch (args.Kind)
            {
                case ProjectChangeKind.DocumentChanged:
                    if (_projectManager.IsDocumentOpen(args.DocumentFilePath))
                    {
                        var document = args.Newer.GetDocument(args.DocumentFilePath);
                        KeepAlive(document);
                    }
                    else
                    {
                        Release(args.DocumentFilePath);
                    }
                    break;
                case ProjectChangeKind.DocumentRemoved:
                    Release(args.DocumentFilePath);
                    break;
                case ProjectChangeKind.ProjectRemoved:
                    foreach (var filePath in args.Older.DocumentFilePaths)
                    {
                        Release(filePath);
                    }
                    break;
            }
        }

        private void KeepAlive(DocumentSnapshot snapshot)
        {
            var defaultSnapshot = snapshot as DefaultDocumentSnapshot;
            if (defaultSnapshot == null)
            {
                return;
            }

            // References the task that's responsible for calculating document output. DocumentSnapshot's don't know about
            // other documents (they shouldn't) so because of this we do the work of referencing generated output for the
            // latest versions of documents that are open so that those documents don't have to re-compute their output.
            // This way those syntax trees will be instantly available.
            _referencedOutputTasks[defaultSnapshot.FilePath] = defaultSnapshot.State.GetGeneratedOutputAndVersionAsync(defaultSnapshot.ProjectInternal, defaultSnapshot);
        }

        private void Release(string filePath)
        {
            // We don't want to reference documents forever, if a document gets removed or closed we release to ensure that the
            // output of the document can get garbage collected.
            _referencedOutputTasks.Remove(filePath);
        }
    }
}
