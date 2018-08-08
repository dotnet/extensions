// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;

namespace Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem
{
    internal class DefaultProjectResolver : ProjectResolver
    {
        // Internal for testing
        protected internal readonly HostProjectShim _miscellaneousHostProject;

        private readonly ForegroundDispatcherShim _foregroundDispatcher;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly ProjectSnapshotManagerShimAccessor _projectSnapshotManagerAccessor;

        public DefaultProjectResolver(
            ForegroundDispatcherShim foregroundDispatcher,
            FilePathNormalizer filePathNormalizer,
            RazorConfigurationResolver configurationResolver,
            ProjectSnapshotManagerShimAccessor projectSnapshotManagerAccessor)
        {
            if (foregroundDispatcher == null)
            {
                throw new ArgumentNullException(nameof(foregroundDispatcher));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (configurationResolver == null)
            {
                throw new ArgumentNullException(nameof(configurationResolver));
            }

            if (projectSnapshotManagerAccessor == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            _foregroundDispatcher = foregroundDispatcher;
            _filePathNormalizer = filePathNormalizer;
            _projectSnapshotManagerAccessor = projectSnapshotManagerAccessor;
            _miscellaneousHostProject = HostProjectShim.Create("__MISC_RAZOR_PROJECT__", configurationResolver.Default);
        }

        public override bool TryResolveProject(string documentFilePath, out ProjectSnapshotShim projectSnapshot)
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

                var projectDirectory = _filePathNormalizer.Normalize(new FileInfo(projects[i].FilePath).Directory.FullName);
                if (normalizedDocumentPath.StartsWith(projectDirectory))
                {
                    projectSnapshot = projects[i];
                    return true;
                }
            }

            projectSnapshot = null;
            return false;
        }

        public override ProjectSnapshotShim GetMiscellaneousProject()
        {
            _foregroundDispatcher.AssertForegroundThread();

            var miscellaneousProject = _projectSnapshotManagerAccessor.Instance.GetLoadedProject(_miscellaneousHostProject.FilePath);
            if (miscellaneousProject == null)
            {
                _projectSnapshotManagerAccessor.Instance.HostProjectAdded(_miscellaneousHostProject);
                miscellaneousProject = _projectSnapshotManagerAccessor.Instance.GetLoadedProject(_miscellaneousHostProject.FilePath);
            }

            return miscellaneousProject;
        }
    }
}
