// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common
{
    internal class TestProjectSnapshot : DefaultProjectSnapshot
    {
        public static TestProjectSnapshot Create(string filePath) => Create(filePath, new string[0]);

        public static TestProjectSnapshot Create(string filePath, string[] documentFilePaths) =>
            Create(filePath, documentFilePaths, RazorConfiguration.Default, projectWorkspaceState: null);

        public static TestProjectSnapshot Create(
            string filePath,
            string[] documentFilePaths,
            RazorConfiguration configuration,
            ProjectWorkspaceState projectWorkspaceState)
        {
            var workspaceServices = new List<IWorkspaceService>()
            {
                new TestProjectSnapshotProjectEngineFactory(),
            };
            var languageServices = new List<ILanguageService>();

            var hostServices = TestServices.Create(workspaceServices, languageServices);
            var workspace = TestWorkspace.Create(hostServices);
            var hostProject = new HostProject(filePath, configuration, "TestRootNamespace");
            var state = ProjectState.Create(workspace.Services, hostProject);
            foreach (var documentFilePath in documentFilePaths)
            {
                var hostDocument = new HostDocument(documentFilePath, documentFilePath);
                state = state.WithAddedHostDocument(hostDocument, () => Task.FromResult(TextAndVersion.Create(SourceText.From(string.Empty), VersionStamp.Default)));
            }

            if (projectWorkspaceState != null)
            {
                state = state.WithProjectWorkspaceState(projectWorkspaceState);
            }

            var testProject = new TestProjectSnapshot(state);

            return testProject;
        }


        private TestProjectSnapshot(ProjectState projectState)
            : base(projectState)
        {
            if (projectState == null)
            {
                throw new ArgumentNullException(nameof(projectState));
            }
        }

        public override VersionStamp Version => throw new NotImplementedException();

        public override DocumentSnapshot GetDocument(string filePath)
        {
            var document = base.GetDocument(filePath);

            if (document == null)
            {
                throw new InvalidOperationException("Test was not setup correctly. Could not locate document '" + filePath + "'.");
            }

            return document;
        }

        public override RazorProjectEngine GetProjectEngine()
        {
            throw new NotImplementedException();
        }
    }
}
