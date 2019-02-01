// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test
{
    internal class TestProjectWorkspaceStateGenerator : ProjectWorkspaceStateGenerator
    {
        private List<(Project workspaceProject, ProjectSnapshot projectSnapshot)> _updates;

        public TestProjectWorkspaceStateGenerator()
        {
            _updates = new List<(Project workspaceProject, ProjectSnapshot projectSnapshot)>();
        }

        public IReadOnlyList<(Project workspaceProject, ProjectSnapshot projectSnapshot)> UpdateQueue => _updates;

        public override void Initialize(ProjectSnapshotManagerBase projectManager)
        {
        }

        public override void Update(Project workspaceProject, ProjectSnapshot projectSnapshot)
        {
            var update = (workspaceProject, projectSnapshot);
            _updates.Add(update);
        }
    }
}
