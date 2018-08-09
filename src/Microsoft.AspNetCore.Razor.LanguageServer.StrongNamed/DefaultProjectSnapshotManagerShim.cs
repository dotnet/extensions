// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    internal class DefaultProjectSnapshotManagerShim : ProjectSnapshotManagerShim
    {
        public DefaultProjectSnapshotManagerShim(ProjectSnapshotManagerBase projectSnapshotManager)
        {
            if (projectSnapshotManager == null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManager));
            }

            InnerProjectSnapshotManager = projectSnapshotManager;

            InnerProjectSnapshotManager.Changed += OnInnerSnapshotManagerChanged;
        }

        public ProjectSnapshotManagerBase InnerProjectSnapshotManager { get; }

        public override IReadOnlyList<ProjectSnapshotShim> Projects
        {
            get
            {
                var projects = new List<ProjectSnapshotShim>();

                for (var i = 0; i < InnerProjectSnapshotManager.Projects.Count; i++)
                {
                    var projectShim = new DefaultProjectSnapshotShim(InnerProjectSnapshotManager.Projects[i]);
                    projects.Add(projectShim);
                }

                return projects;
            }
        }

        public override Workspace Workspace => InnerProjectSnapshotManager.Workspace;

#pragma warning disable 67
        public override event EventHandler<ProjectChangeEventArgsShim> Changed;
#pragma warning restore 67

        public override void DocumentAdded(HostProjectShim hostProject, HostDocumentShim hostDocument, TextLoader textLoader)
        {
            if (!(hostProject is DefaultHostProjectShim defaultHostProject))
            {
                throw new ArgumentException("Cannot understand non-default implementations of host project.");
            }

            if (!(hostDocument is DefaultHostDocumentShim defaultHostDocument))
            {
                throw new ArgumentException("Cannot understand non-default implementations of host document.");
            }

            InnerProjectSnapshotManager.DocumentAdded(defaultHostProject.InnerHostProject, defaultHostDocument.InnerHostDocument, textLoader);
        }

        public override void DocumentChanged(string projectFilePath, string documentFilePath, TextLoader textLoader) => InnerProjectSnapshotManager.DocumentChanged(projectFilePath, documentFilePath, textLoader);

        public override void DocumentChanged(string projectFilePath, string documentFilePath, SourceText sourceText) => InnerProjectSnapshotManager.DocumentChanged(projectFilePath, documentFilePath, sourceText);

        public override void DocumentClosed(string projectFilePath, string documentFilePath, TextLoader textLoader) => InnerProjectSnapshotManager.DocumentClosed(projectFilePath, documentFilePath, textLoader);

        public override void DocumentOpened(string projectFilePath, string documentFilePath, SourceText sourceText) => InnerProjectSnapshotManager.DocumentOpened(projectFilePath, documentFilePath, sourceText);

        public override void DocumentRemoved(HostProjectShim hostProject, HostDocumentShim hostDocument)
        {
            if (!(hostProject is DefaultHostProjectShim defaultHostProject))
            {
                throw new ArgumentException("Cannot understand non-default implementations of host project.");
            }

            if (!(hostDocument is DefaultHostDocumentShim defaultHostDocument))
            {
                throw new ArgumentException("Cannot understand non-default implementations of host document.");
            }

            InnerProjectSnapshotManager.DocumentRemoved(defaultHostProject.InnerHostProject, defaultHostDocument.InnerHostDocument);
        }

        public override ProjectSnapshotShim GetLoadedProject(string filePath)
        {
            var loadedProject = InnerProjectSnapshotManager.GetLoadedProject(filePath);

            if (loadedProject == null)
            {
                return null;
            }

            return new DefaultProjectSnapshotShim(loadedProject);
        }

        public override ProjectSnapshotShim GetOrCreateProject(string filePath)
        {
            var project = InnerProjectSnapshotManager.GetOrCreateProject(filePath);

            return new DefaultProjectSnapshotShim(project);
        }

        public override void HostProjectAdded(HostProjectShim hostProject)
        {
            if (!(hostProject is DefaultHostProjectShim defaultHostProject))
            {
                throw new ArgumentException("Cannot understand non-default implementations of host project.");
            }

            InnerProjectSnapshotManager.HostProjectAdded(defaultHostProject.InnerHostProject);
        }

        public override void HostProjectChanged(HostProjectShim hostProject)
        {
            if (!(hostProject is DefaultHostProjectShim defaultHostProject))
            {
                throw new ArgumentException("Cannot understand non-default implementations of host project.");
            }

            InnerProjectSnapshotManager.HostProjectChanged(defaultHostProject.InnerHostProject);
        }

        public override void HostProjectRemoved(HostProjectShim hostProject)
        {
            if (!(hostProject is DefaultHostProjectShim defaultHostProject))
            {
                throw new ArgumentException("Cannot understand non-default implementations of host project.");
            }

            InnerProjectSnapshotManager.HostProjectRemoved(defaultHostProject.InnerHostProject);
        }

        public override bool IsDocumentOpen(string documentFilePath) => InnerProjectSnapshotManager.IsDocumentOpen(documentFilePath);

        public override void ReportError(Exception exception) => InnerProjectSnapshotManager.ReportError(exception);

        public override void ReportError(Exception exception, ProjectSnapshotShim project)
        {
            if (!(project is DefaultProjectSnapshotShim defaultProjectSnapshot))
            {
                throw new ArgumentException("Cannot understand non-default implementations of project snapshot.");
            }

            InnerProjectSnapshotManager.ReportError(exception, defaultProjectSnapshot.InnerProjectSnapshot);
        }

        public override void ReportError(Exception exception, HostProjectShim hostProject)
        {
            if (!(hostProject is DefaultHostProjectShim defaultHostProject))
            {
                throw new ArgumentException("Cannot understand non-default implementations of host project.");
            }

            InnerProjectSnapshotManager.ReportError(exception, defaultHostProject.InnerHostProject);
        }

        public override void ReportError(Exception exception, Project workspaceProject) => InnerProjectSnapshotManager.ReportError(exception, workspaceProject);

        public override void WorkspaceProjectAdded(Project workspaceProject) => InnerProjectSnapshotManager.WorkspaceProjectAdded(workspaceProject);

        public override void WorkspaceProjectChanged(Project workspaceProject) => InnerProjectSnapshotManager.WorkspaceProjectChanged(workspaceProject);

        public override void WorkspaceProjectRemoved(Project workspaceProject) => InnerProjectSnapshotManager.WorkspaceProjectRemoved(workspaceProject);

        private void OnInnerSnapshotManagerChanged(object sender, ProjectChangeEventArgs args)
        {
            var shimArgs = new ProjectChangeEventArgsShim(args.ProjectFilePath, args.DocumentFilePath, (ProjectChangeKindShim)args.Kind);

            Changed?.Invoke(this, shimArgs);
        }
    }
}
