// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using OmniSharp;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public abstract class OmniSharpWorkspaceTestBase : OmniSharpTestBase
    {
        public OmniSharpWorkspaceTestBase()
        {
            Workspace = TestOmniSharpWorkspace.Create();
            var projectId = ProjectId.CreateNewId();
            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Default, "TestProject", "TestAssembly", LanguageNames.CSharp, filePath: "/path/to/project.csproj");
            Workspace.AddProject(projectInfo);
            Project = Workspace.CurrentSolution.Projects.FirstOrDefault();
        }

        protected OmniSharpWorkspace Workspace { get; }

        protected Project Project { get; }

        protected Document AddRoslynDocument(string filePath)
        {
            var backgroundDocumentId = DocumentId.CreateNewId(Project.Id);
            var backgroundDocumentInfo = DocumentInfo.Create(backgroundDocumentId, filePath ?? "EmptyFile", filePath: filePath);
            Workspace.AddDocument(backgroundDocumentInfo);
            var addedDocument = Workspace.CurrentSolution.GetDocument(backgroundDocumentId);
            return addedDocument;
        }
    }
}
