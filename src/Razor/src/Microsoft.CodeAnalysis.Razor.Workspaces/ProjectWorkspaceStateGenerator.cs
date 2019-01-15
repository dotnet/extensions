// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor
{
    internal abstract class ProjectWorkspaceStateGenerator : ProjectSnapshotChangeTrigger
    {
        public abstract void Update(Project workspaceProject, ProjectSnapshot projectSnapshot);
    }
}