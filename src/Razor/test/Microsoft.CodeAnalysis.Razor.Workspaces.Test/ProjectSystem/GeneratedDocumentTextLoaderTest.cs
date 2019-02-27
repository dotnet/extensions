// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class GeneratedDocumentTextLoaderTest : WorkspaceTestBase
    {
        public GeneratedDocumentTextLoaderTest()
        {
            HostProject = TestProjectData.SomeProject;
            HostDocument = TestProjectData.SomeProjectFile1;
        }

        private HostProject HostProject { get; }
        private HostDocument HostDocument { get; }

        // See https://github.com/aspnet/AspNetCore/issues/7997
        [Fact]
        public async Task LoadAsync_SpecifiesEncoding()
        {
            // Arrange
            var project = new DefaultProjectSnapshot(
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, () =>
                {
                    return Task.FromResult(TextAndVersion.Create(SourceText.From(""), VersionStamp.Create()));
                }));

            var document = project.GetDocument(HostDocument.FilePath);

            var loader = new GeneratedDocumentTextLoader(document, "file.cshtml");

            // Act
            var textAndVersion = await loader.LoadTextAndVersionAsync(default, default, default);

            // Assert
            Assert.True(textAndVersion.Text.CanBeEmbedded);
            Assert.Same(Encoding.UTF8, textAndVersion.Text.Encoding);
        }
    }
}
