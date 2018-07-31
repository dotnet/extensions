// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    public abstract class ProjectSnapshotManagerShim
    {
        public abstract event EventHandler<ProjectChangeEventArgsShim> Changed;

        public abstract IReadOnlyList<ProjectSnapshotShim> Projects { get; }

        public abstract bool IsDocumentOpen(string documentFilePath);

        public abstract ProjectSnapshotShim GetLoadedProject(string filePath);

        public abstract ProjectSnapshotShim GetOrCreateProject(string filePath);


        public abstract Workspace Workspace { get; }

        public abstract void DocumentAdded(HostProjectShim hostProject, HostDocumentShim hostDocument, TextLoader textLoader);

        public abstract void DocumentOpened(string projectFilePath, string documentFilePath, SourceText sourceText);

        public abstract void DocumentClosed(string projectFilePath, string documentFilePath, TextLoader textLoader);

        public abstract void DocumentChanged(string projectFilePath, string documentFilePath, TextLoader textLoader);

        public abstract void DocumentChanged(string projectFilePath, string documentFilePath, SourceText sourceText);

        public abstract void DocumentRemoved(HostProjectShim hostProject, HostDocumentShim hostDocument);

        public abstract void HostProjectAdded(HostProjectShim hostProject);

        public abstract void HostProjectChanged(HostProjectShim hostProject);

        public abstract void HostProjectRemoved(HostProjectShim hostProject);

        public abstract void WorkspaceProjectAdded(Project workspaceProject);

        public abstract void WorkspaceProjectChanged(Project workspaceProject);

        public abstract void WorkspaceProjectRemoved(Project workspaceProject);

        public abstract void ReportError(Exception exception);

        public abstract void ReportError(Exception exception, ProjectSnapshotShim project);

        public abstract void ReportError(Exception exception, HostProjectShim hostProject);

        public abstract void ReportError(Exception exception, Project workspaceProject);
    }
}
