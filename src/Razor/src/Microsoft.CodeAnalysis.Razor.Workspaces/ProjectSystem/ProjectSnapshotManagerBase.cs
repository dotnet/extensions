// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectSnapshotManagerBase : ProjectSnapshotManager
    {
        public abstract Workspace Workspace { get; }

        public abstract void DocumentAdded(HostProject hostProject, HostDocument hostDocument, TextLoader textLoader);

        public abstract void DocumentOpened(string projectFilePath, string documentFilePath, SourceText sourceText);

        public abstract void DocumentClosed(string projectFilePath, string documentFilePath, TextLoader textLoader);

        public abstract void DocumentChanged(string projectFilePath, string documentFilePath, TextLoader textLoader);

        public abstract void DocumentChanged(string projectFilePath, string documentFilePath, SourceText sourceText);

        public abstract void DocumentRemoved(HostProject hostProject, HostDocument hostDocument);

        public abstract void ProjectAdded(HostProject hostProject);

        public abstract void ProjectConfigurationChanged(HostProject hostProject);

        public abstract void ProjectWorkspaceStateChanged(string projectFilePath, ProjectWorkspaceState projectWorkspaceState);

        public abstract void ProjectRemoved(HostProject hostProject);

        public abstract void ReportError(Exception exception);

        public abstract void ReportError(Exception exception, ProjectSnapshot project);

        public abstract void ReportError(Exception exception, HostProject hostProject);
    }
}