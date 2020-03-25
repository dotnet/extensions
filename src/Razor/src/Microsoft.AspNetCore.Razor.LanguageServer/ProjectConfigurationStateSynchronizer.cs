// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class ProjectConfigurationStateSynchronizer : IProjectConfigurationFileChangeListener
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly RazorProjectService _projectService;
        private readonly Dictionary<string, string> _configurationToProjectMap;

        public ProjectConfigurationStateSynchronizer(
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
            _configurationToProjectMap = new Dictionary<string, string>(FilePathComparer.Instance);
        }

        public void ProjectConfigurationFileChanged(ProjectConfigurationFileChangeEventArgs args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            _foregroundDispatcher.AssertForegroundThread();

            switch (args.Kind)
            {
                case RazorFileChangeKind.Changed:
                    {
                        if (!args.TryDeserialize(out var handle))
                        {
                            if (!_configurationToProjectMap.TryGetValue(args.ConfigurationFilePath, out var projectFilePath))
                            {
                                // Could not resolve an associated project file, noop.
                                return;
                            }

                            // We found the last associated project file for the configuration file. Reset the project since we can't
                            // accurately determine its configurations.
                            ResetProject(projectFilePath);
                            return;
                        }

                        UpdateProject(handle);
                        break;
                    }
                case RazorFileChangeKind.Added:
                    {
                        if (!args.TryDeserialize(out var handle))
                        {
                            // Given that this is the first time we're seeing this configuration file if we can't deserialize it
                            // then we have to noop.
                            return;
                        }

                        var projectFilePath = handle.FilePath;
                        _configurationToProjectMap[args.ConfigurationFilePath] = projectFilePath;
                        _projectService.AddProject(projectFilePath);
                        UpdateProject(handle);
                        break;
                    }
                case RazorFileChangeKind.Removed:
                    {
                        var containsKey = _configurationToProjectMap.TryGetValue(args.ConfigurationFilePath, out var projectFilePath);
                        Debug.Assert(containsKey);

                        _configurationToProjectMap.Remove(args.ConfigurationFilePath);

                        ResetProject(projectFilePath);
                        break;
                    }
            }

            void UpdateProject(FullProjectSnapshotHandle handle)
            {
                if (handle is null)
                {
                    throw new ArgumentNullException(nameof(handle));
                }

                var projectWorkspaceState = handle.ProjectWorkspaceState ?? ProjectWorkspaceState.Default;
                var documents = handle.Documents ?? Array.Empty<DocumentSnapshotHandle>();
                _projectService.UpdateProject(
                    handle.FilePath,
                    handle.Configuration,
                    handle.RootNamespace,
                    projectWorkspaceState,
                    documents);
            }

            void ResetProject(string projectFilePath)
            {
                _projectService.UpdateProject(
                    projectFilePath,
                    configuration: null,
                    rootNamespace: null,
                    ProjectWorkspaceState.Default,
                    Array.Empty<DocumentSnapshotHandle>());
            }
        }
    }
}
