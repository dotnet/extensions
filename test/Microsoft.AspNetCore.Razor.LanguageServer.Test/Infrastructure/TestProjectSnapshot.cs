// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Infrastructure
{
    internal class TestProjectSnapshot : DefaultProjectSnapshot
    {
        public static TestProjectSnapshot Create(string filePath) => Create(filePath, new string[0]);

        public static TestProjectSnapshot Create(string filePath, string[] documentFilePaths)
        {
            var workspace = TestWorkspace.Create();
            var hostProject = new HostProject(filePath, RazorConfiguration.Default);
            var state = ProjectState.Create(workspace.Services, hostProject);
            foreach (var documentFilePath in documentFilePaths)
            {
                var hostDocument = new HostDocument(documentFilePath, documentFilePath);
                state = state.WithAddedHostDocument(hostDocument, () => Task.FromResult(TextAndVersion.Create(SourceText.From(string.Empty), VersionStamp.Default)));
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

        public override bool IsInitialized => throw new NotImplementedException();

        public override VersionStamp Version => throw new NotImplementedException();

        public override Project WorkspaceProject => throw new NotImplementedException();

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

        public override Task<IReadOnlyList<TagHelperDescriptor>> GetTagHelpersAsync() => Task.FromResult<IReadOnlyList<TagHelperDescriptor>>(Array.Empty<TagHelperDescriptor>());

        public override bool TryGetTagHelpers(out IReadOnlyList<TagHelperDescriptor> result)
        {
            result = Array.Empty<TagHelperDescriptor>();
            return true;
        }
    }
}
