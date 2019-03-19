// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    // Internal tracker for DefaultProjectSnapshot
    internal class ProjectState
    {
        private const ProjectDifference ClearConfigurationVersionMask = ProjectDifference.ConfigurationChanged;

        private const ProjectDifference ClearProjectWorkspaceStateVersionMask =
            ProjectDifference.ConfigurationChanged |
            ProjectDifference.ProjectWorkspaceStateChanged;

        private const ProjectDifference ClearDocumentCollectionVersionMask =
            ProjectDifference.ConfigurationChanged |
            ProjectDifference.DocumentAdded |
            ProjectDifference.DocumentRemoved;

        private static readonly ImmutableDictionary<string, DocumentState> EmptyDocuments = ImmutableDictionary.Create<string, DocumentState>(FilePathComparer.Instance);
        private static readonly ImmutableDictionary<string, ImmutableArray<string>> EmptyImportsToRelatedDocuments = ImmutableDictionary.Create<string, ImmutableArray<string>>(FilePathComparer.Instance);
        private static readonly IReadOnlyList<TagHelperDescriptor> EmptyTagHelpers = Array.Empty<TagHelperDescriptor>();
        private readonly object _lock;

        private RazorProjectEngine _projectEngine;

        public static ProjectState Create(
            HostWorkspaceServices services,
            HostProject hostProject,
            ProjectWorkspaceState projectWorkspaceState = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            return new ProjectState(services, hostProject, projectWorkspaceState);
        }

        private ProjectState(
            HostWorkspaceServices services,
            HostProject hostProject,
            ProjectWorkspaceState projectWorkspaceState)
        {
            Services = services;
            HostProject = hostProject;
            ProjectWorkspaceState = projectWorkspaceState;
            Documents = EmptyDocuments;
            ImportsToRelatedDocuments = EmptyImportsToRelatedDocuments;
            Version = VersionStamp.Create();
            DocumentCollectionVersion = Version;

            _lock = new object();
        }

        private ProjectState(
            ProjectState older,
            ProjectDifference difference,
            HostProject hostProject,
            ProjectWorkspaceState projectWorkspaceState,
            ImmutableDictionary<string, DocumentState> documents,
            ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments)
        {
            if (older == null)
            {
                throw new ArgumentNullException(nameof(older));
            }

            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            if (importsToRelatedDocuments == null)
            {
                throw new ArgumentNullException(nameof(importsToRelatedDocuments));
            }

            Services = older.Services;
            Version = older.Version.GetNewerVersion();

            HostProject = hostProject;
            ProjectWorkspaceState = projectWorkspaceState;
            Documents = documents;
            ImportsToRelatedDocuments = importsToRelatedDocuments;

            _lock = new object();

            if ((difference & ClearDocumentCollectionVersionMask) == 0)
            {
                // Document collection hasn't changed
                DocumentCollectionVersion = older.DocumentCollectionVersion;
            }
            else
            {
                DocumentCollectionVersion = Version;
            }

            if ((difference & ClearConfigurationVersionMask) == 0 && older._projectEngine != null)
            {
                // Optimistically cache the RazorProjectEngine.
                _projectEngine = older.ProjectEngine;
                ConfigurationVersion = older.ConfigurationVersion;
            }
            else
            {
                ConfigurationVersion = Version;
            }   

            if ((difference & ClearProjectWorkspaceStateVersionMask) == 0 ||
                ProjectWorkspaceState == older.ProjectWorkspaceState ||
                ProjectWorkspaceState?.Equals(older.ProjectWorkspaceState) == true)
            {
                ProjectWorkspaceStateVersion = older.ProjectWorkspaceStateVersion;
            }
            else
            {
                ProjectWorkspaceStateVersion = Version;
            }
        }

        // Internal set for testing.
        public ImmutableDictionary<string, DocumentState> Documents { get; internal set; }

        // Internal set for testing.
        public ImmutableDictionary<string, ImmutableArray<string>> ImportsToRelatedDocuments { get; internal set; }

        public HostProject HostProject { get; }

        public ProjectWorkspaceState ProjectWorkspaceState { get; }

        public HostWorkspaceServices Services { get; }

        public IReadOnlyList<TagHelperDescriptor> TagHelpers => ProjectWorkspaceState?.TagHelpers ?? EmptyTagHelpers;

        public LanguageVersion CSharpLanguageVersion => ProjectWorkspaceState?.CSharpLanguageVersion ?? LanguageVersion.Default;

        /// <summary>
        /// Gets the version of this project, INCLUDING content changes. The <see cref="Version"/> is
        /// incremented for each new <see cref="ProjectState"/> instance created.
        /// </summary>
        public VersionStamp Version { get; }

        /// <summary>
        /// Gets the version of this project, NOT INCLUDING computed or content changes. The
        /// <see cref="DocumentCollectionVersion"/> is incremented each time the configuration changes or
        /// a document is added or removed.
        /// </summary>
        public VersionStamp DocumentCollectionVersion { get; }

        public RazorProjectEngine ProjectEngine
        {
            get
            {
                lock (_lock)
                {
                    if (_projectEngine == null)
                    {
                        _projectEngine = this.CreateProjectEngine();
                    }
                }

                return _projectEngine;
            }

        }

        /// <summary>
        /// Gets the version of this project based on the project workspace state, NOT INCLUDING content
        /// changes. The computed state is guaranteed to change when the configuration or tag helpers
        /// change.
        /// </summary>
        public VersionStamp ProjectWorkspaceStateVersion { get; }

        public VersionStamp ConfigurationVersion { get; }

        public ProjectState WithAddedHostDocument(HostDocument hostDocument, Func<Task<TextAndVersion>> loader)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            // Ignore attempts to 'add' a document with different data, we only
            // care about one, so it might as well be the one we have.
            if (Documents.ContainsKey(hostDocument.FilePath))
            {
                return this;
            }

            var documents = Documents.Add(hostDocument.FilePath, DocumentState.Create(Services, hostDocument, loader));

            // Compute the effect on the import map
            var importTargetPaths = GetImportDocumentTargetPaths(hostDocument.TargetPath);
            var importsToRelatedDocuments = AddToImportsToRelatedDocuments(ImportsToRelatedDocuments, hostDocument, importTargetPaths);

            // Now check if the updated document is an import - it's important this this happens after
            // updating the imports map.
            if (importsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
            {
                foreach (var relatedDocument in relatedDocuments)
                {
                    documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
                }
            }

            var state = new ProjectState(this, ProjectDifference.DocumentAdded, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
            return state;
        }

        public ProjectState WithRemovedHostDocument(HostDocument hostDocument)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            if (!Documents.ContainsKey(hostDocument.FilePath))
            {
                return this;
            }

            var documents = Documents.Remove(hostDocument.FilePath);

            // First check if the updated document is an import - it's important that this happens
            // before updating the imports map.
            if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
            {
                foreach (var relatedDocument in relatedDocuments)
                {
                    documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
                }
            }

            // Compute the effect on the import map
            var importTargetPaths = GetImportDocumentTargetPaths(hostDocument.TargetPath);
            var importsToRelatedDocuments = RemoveFromImportsToRelatedDocuments(ImportsToRelatedDocuments, hostDocument, importTargetPaths);

            var state = new ProjectState(this, ProjectDifference.DocumentRemoved, HostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
            return state;
        }

        public ProjectState WithChangedHostDocument(HostDocument hostDocument, SourceText sourceText, VersionStamp version)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
            {
                return this;
            }

            var documents = Documents.SetItem(hostDocument.FilePath, document.WithText(sourceText, version));

            if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
            {
                foreach (var relatedDocument in relatedDocuments)
                {
                    documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
                }
            }

            var state = new ProjectState(this, ProjectDifference.DocumentChanged, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments);
            return state;
        }

        public ProjectState WithChangedHostDocument(HostDocument hostDocument, Func<Task<TextAndVersion>> loader)
        {
            if (hostDocument == null)
            {
                throw new ArgumentNullException(nameof(hostDocument));
            }

            if (!Documents.TryGetValue(hostDocument.FilePath, out var document))
            {
                return this;
            }

            var documents = Documents.SetItem(hostDocument.FilePath, document.WithTextLoader(loader));

            if (ImportsToRelatedDocuments.TryGetValue(hostDocument.TargetPath, out var relatedDocuments))
            {
                foreach (var relatedDocument in relatedDocuments)
                {
                    documents = documents.SetItem(relatedDocument, documents[relatedDocument].WithImportsChange());
                }
            }

            var state = new ProjectState(this, ProjectDifference.DocumentChanged, HostProject, ProjectWorkspaceState, documents, ImportsToRelatedDocuments);
            return state;
        }

        public ProjectState WithHostProject(HostProject hostProject)
        {
            if (hostProject == null)
            {
                throw new ArgumentNullException(nameof(hostProject));
            }

            if (HostProject.Configuration.Equals(hostProject.Configuration))
            {
                return this;
            }

            var documents = Documents.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.WithConfigurationChange(), FilePathComparer.Instance);

            // If the host project has changed then we need to recompute the imports map
            var importsToRelatedDocuments = EmptyImportsToRelatedDocuments;

            foreach (var document in documents)
            {
                var importTargetPaths = GetImportDocumentTargetPaths(document.Value.HostDocument.TargetPath);
                importsToRelatedDocuments = AddToImportsToRelatedDocuments(ImportsToRelatedDocuments, document.Value.HostDocument, importTargetPaths);
            }

            var state = new ProjectState(this, ProjectDifference.ConfigurationChanged, hostProject, ProjectWorkspaceState, documents, importsToRelatedDocuments);
            return state;
        }

        public ProjectState WithProjectWorkspaceState(ProjectWorkspaceState projectWorkspaceState)
        {
            var difference = ProjectDifference.ProjectWorkspaceStateChanged;

            var documents = Documents.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.WithProjectWorkspaceStateChange(), FilePathComparer.Instance);
            var state = new ProjectState(this, difference, HostProject, projectWorkspaceState, documents, ImportsToRelatedDocuments);
            return state;
        }

        private static ImmutableDictionary<string, ImmutableArray<string>> AddToImportsToRelatedDocuments(
            ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments,
            HostDocument hostDocument,
            List<string> importTargetPaths)
        {
            foreach (var importTargetPath in importTargetPaths)
            {
                if (!importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
                {
                    relatedDocuments = ImmutableArray.Create<string>();
                }

                relatedDocuments = relatedDocuments.Add(hostDocument.FilePath);
                importsToRelatedDocuments = importsToRelatedDocuments.SetItem(importTargetPath, relatedDocuments);
            }

            return importsToRelatedDocuments;
        }

        private static ImmutableDictionary<string, ImmutableArray<string>> RemoveFromImportsToRelatedDocuments(
            ImmutableDictionary<string, ImmutableArray<string>> importsToRelatedDocuments,
            HostDocument hostDocument,
            List<string> importTargetPaths)
        {
            foreach (var importTargetPath in importTargetPaths)
            {
                if (importsToRelatedDocuments.TryGetValue(importTargetPath, out var relatedDocuments))
                {
                    relatedDocuments = relatedDocuments.Remove(hostDocument.FilePath);
                    if (relatedDocuments.Length > 0)
                    {
                        importsToRelatedDocuments = importsToRelatedDocuments.SetItem(importTargetPath, relatedDocuments);
                    }
                    else
                    {
                        importsToRelatedDocuments = importsToRelatedDocuments.Remove(importTargetPath);
                    }
                }
            }

            return importsToRelatedDocuments;
        }

        private RazorProjectEngine CreateProjectEngine()
        {
            var factory = Services.GetRequiredService<ProjectSnapshotProjectEngineFactory>();
            return factory.Create(
                HostProject.Configuration,
                Path.GetDirectoryName(HostProject.FilePath),
                configure: builder =>
                {
                    builder.SetRootNamespace(HostProject.RootNamespace);
                    builder.SetCSharpLanguageVersion(CSharpLanguageVersion);
                });
        }

        public List<string> GetImportDocumentTargetPaths(string targetPath)
        {
            var projectEngine = ProjectEngine;
            var importFeatures = projectEngine.ProjectFeatures.OfType<IImportProjectFeature>();
            var projectItem = projectEngine.FileSystem.GetItem(targetPath);
            var importItems = importFeatures.SelectMany(f => f.GetImports(projectItem)).Where(i => i.FilePath != null);

            // Target path looks like `Foo\\Bar.cshtml`
            var targetPaths = new List<string>();
            foreach (var importItem in importItems)
            {
                var itemTargetPath = importItem.FilePath.Replace('/', '\\').TrimStart('\\');
                targetPaths.Add(itemTargetPath);
            }

            return targetPaths;
        }
    }
}
