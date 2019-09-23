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

        public override bool TryResolvePotentialProject(string documentFilePath, out ProjectSnapshot projectSnapshot)
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
                if (projects[i].FilePath == _miscellaneousHostProject.FilePath)
                {
                    // We don't resolve documents to belonging to the miscellaneous project.
                    continue;
                }

                var projectDirectory = _filePathNormalizer.GetDirectory(projects[i].FilePath);
                if (normalizedDocumentPath.StartsWith(projectDirectory))
                {
                    projectSnapshot = projects[i];
                    return true;
                }
            }

            projectSnapshot = null;
            return false;
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
