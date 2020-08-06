// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class DefaultProjectResolver : ProjectResolver
    {
        // Internal for testing
        protected internal readonly HostProject _miscellaneousHostProject;

        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly ProjectSnapshotManagerAccessor _projectSnapshotManagerAccessor;

        public DefaultProjectResolver(
            ForegroundDispatcher foregroundDispatcher,
            FilePathNormalizer filePathNormalizer,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (projectSnapshotManagerAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _filePathNormalizer = filePathNormalizer;
            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;

            var miscellaneousProjectPath = Path.Combine(TempDirectory.Instance.DirectoryPath, "__MISC_RAZOR_PROJECT__");
            _miscellaneousHostProject = new HostProject(miscellaneousProjectPath, RazorDefaults.Configuration, RazorDefaults.RootNamespace);
        }

        public override bool TryResolveProject(string documentFilePath, out ProjectSnapshot projectSnapshot, bool enforceDocumentInProject = true)
        {
            if (documentFilePath == null)
            {
                throw new ArgumentNullException(nameof(documentFilePath));
            }

            _foregroundDispatcher.AssertForegroundThread();

            var normalizedDocumentPath = _filePathNormalizer.Normalize(documentFilePath);
            var projects = _projectSnapshotManagerAccessor.Instance.Projects;
            for (var i = 0; i < projects.Count; i++)
            {
                projectSnapshot = projects[i];

                if (projectSnapshot.FilePath == _miscellaneousHostProject.FilePath)
                {
                    if (enforceDocumentInProject &&
                        IsDocumentInProject(projectSnapshot, documentFilePath))
                    {
                        return true;
                    }

                    continue;
                }

                var projectDirectory = _filePathNormalizer.GetDirectory(projectSnapshot.FilePath);
                if (normalizedDocumentPath.StartsWith(projectDirectory, FilePathComparison.Instance) &&
                    (!enforceDocumentInProject || IsDocumentInProject(projectSnapshot, documentFilePath)))
                {
                    return true;
                }
            }

            projectSnapshot = null;
            return false;

            static bool IsDocumentInProject(ProjectSnapshot projectSnapshot, string documentFilePath) =>
                projectSnapshot.GetDocument(documentFilePath) != null;
        }

        public override ProjectSnapshot GetMiscellaneousProject()
        {
            _foregroundDispatcher.AssertForegroundThread();

            var miscellaneousProject = _projectSnapshotManagerAccessor.Instance.GetLoadedProject(_miscellaneousHostProject.FilePath);
            if (miscellaneousProject == null)
            {
                _projectSnapshotManagerAccessor.Instance.ProjectAdded(_miscellaneousHostProject);
                miscellaneousProject = _projectSnapshotManagerAccessor.Instance.GetLoadedProject(_miscellaneousHostProject.FilePath);
            }

            return miscellaneousProject;
        }
    }
}
