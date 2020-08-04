// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class DefaultDocumentSnapshotTest : WorkspaceTestBase
    {
        public DefaultDocumentSnapshotTest()
        {
            SourceText = SourceText.From("<p>Hello World</p>");
            Version = VersionStamp.Create();

            // Create a new HostDocument to avoid mutating the code container
            ComponentCshtmlHostDocument = new HostDocument(TestProjectData.SomeProjectCshtmlComponentFile5);
            ComponentHostDocument = new HostDocument(TestProjectData.SomeProjectComponentFile1);
            LegacyHostDocument = new HostDocument(TestProjectData.SomeProjectFile1);
            NestedComponentHostDocument = new HostDocument(TestProjectData.SomeProjectNestedComponentFile3);

            var projectState = ProjectState.Create(Workspace.Services, TestProjectData.SomeProject);
            var project = new DefaultProjectSnapshot(projectState);

            var textAndVersion = TextAndVersion.Create(SourceText, Version);

            var documentState = DocumentState.Create(Workspace.Services, LegacyHostDocument, () => Task.FromResult(textAndVersion));
            LegacyDocument = new DefaultDocumentSnapshot(project, documentState);

            documentState = DocumentState.Create(Workspace.Services, ComponentHostDocument, () => Task.FromResult(textAndVersion));
            ComponentDocument = new DefaultDocumentSnapshot(project, documentState);

            documentState = DocumentState.Create(Workspace.Services, ComponentCshtmlHostDocument, () => Task.FromResult(textAndVersion));
            ComponentCshtmlDocument = new DefaultDocumentSnapshot(project, documentState);

            documentState = DocumentState.Create(Workspace.Services, NestedComponentHostDocument, () => Task.FromResult(textAndVersion));
            NestedComponentDocument = new DefaultDocumentSnapshot(project, documentState);
        }

        private SourceText SourceText { get; }

        private VersionStamp Version { get; }

        private HostDocument ComponentHostDocument { get; }

        private HostDocument ComponentCshtmlHostDocument { get; }

        private HostDocument LegacyHostDocument { get; }

        private DefaultDocumentSnapshot ComponentDocument { get; }

        private DefaultDocumentSnapshot ComponentCshtmlDocument { get; }

        private DefaultDocumentSnapshot LegacyDocument { get; }

        private HostDocument NestedComponentHostDocument { get; }

        private DefaultDocumentSnapshot NestedComponentDocument { get; }

        protected override void ConfigureWorkspaceServices(List<IWorkspaceService> services)
        {
            services.Add(new TestTagHelperResolver());
        }

        [Fact]
        public async Task GCCollect_OutputIsNoLongerCached()
        {
            // Arrange
            await LegacyDocument.GetGeneratedOutputAsync();

            // Act

            // Forces collection of the cached document output
            GC.Collect();

            // Assert
            Assert.False(LegacyDocument.TryGetGeneratedOutput(out _));
            Assert.False(LegacyDocument.TryGetGeneratedCSharpOutputVersionAsync(out _));
            Assert.False(LegacyDocument.TryGetGeneratedHtmlOutputVersionAsync(out _));
        }

        [Fact]
        public async Task GCCollect_OnRegenerationMaintainsOutputVersion()
        {
            // Arrange
            var initialOutputVersion = await LegacyDocument.GetGeneratedCSharpOutputVersionAsync();

            // Forces collection of the cached document output
            GC.Collect();

            // Act
            var regeneratedCSharpOutputVersion = await LegacyDocument.GetGeneratedCSharpOutputVersionAsync();
            var regeneratedHtmlOutputVersion = await LegacyDocument.GetGeneratedHtmlOutputVersionAsync();

            // Assert
            Assert.Equal(initialOutputVersion, regeneratedCSharpOutputVersion);
            Assert.Equal(initialOutputVersion, regeneratedHtmlOutputVersion);
        }

        [Fact]
        public async Task RegeneratingWithReference_CachesOutput()
        {
            // Arrange
            var output = await LegacyDocument.GetGeneratedOutputAsync();

            // Mostly doing this to ensure "var output" doesn't get optimized out
            Assert.NotNull(output);

            // Act & Assert
            Assert.True(LegacyDocument.TryGetGeneratedOutput(out _));
            Assert.True(LegacyDocument.TryGetGeneratedCSharpOutputVersionAsync(out _));
            Assert.True(LegacyDocument.TryGetGeneratedHtmlOutputVersionAsync(out _));
        }

        [Fact]
        public async Task GetGeneratedOutputAsync_SetsHostDocumentOutput()
        {
            // Act
            await LegacyDocument.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(LegacyHostDocument.GeneratedDocumentContainer.OutputCSharp);
            Assert.NotNull(LegacyHostDocument.GeneratedDocumentContainer.OutputHtml);
            Assert.Same(SourceText, LegacyHostDocument.GeneratedDocumentContainer.Source);
        }

        // This is a sanity test that we invoke component codegen for components. It's a little fragile but
        // necessary.

        [Fact]
        public async Task GetGeneratedOutputAsync_CshtmlComponent_ContainsComponentImports()
        {
            // Act
            await ComponentCshtmlDocument.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(ComponentCshtmlHostDocument.GeneratedDocumentContainer.OutputCSharp);
            Assert.Contains("using Microsoft.AspNetCore.Components", ComponentCshtmlHostDocument.GeneratedDocumentContainer.OutputCSharp.GeneratedCode, StringComparison.Ordinal);
        }
        [Fact]
        public async Task GetGeneratedOutputAsync_Component()
        {
            // Act
            await ComponentDocument.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(ComponentHostDocument.GeneratedDocumentContainer.OutputCSharp);
            Assert.Contains("ComponentBase", ComponentHostDocument.GeneratedDocumentContainer.OutputCSharp.GeneratedCode, StringComparison.Ordinal);
        }

        [Fact]
        public async Task GetGeneratedOutputAsync_NestedComponentDocument_SetsCorrectNamespaceAndClassName()
        {
            // Act
            await NestedComponentDocument.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(NestedComponentHostDocument.GeneratedDocumentContainer.OutputCSharp);
            Assert.Contains("ComponentBase", NestedComponentHostDocument.GeneratedDocumentContainer.OutputCSharp.GeneratedCode, StringComparison.Ordinal);
            Assert.Contains("namespace SomeProject.Nested", NestedComponentHostDocument.GeneratedDocumentContainer.OutputCSharp.GeneratedCode, StringComparison.Ordinal);
            Assert.Contains("class File3", NestedComponentHostDocument.GeneratedDocumentContainer.OutputCSharp.GeneratedCode, StringComparison.Ordinal);
        }

        // This is a sanity test that we invoke legacy codegen for .cshtml files. It's a little fragile but
        // necessary.
        [Fact]
        public async Task GetGeneratedOutputAsync_Legacy()
        {
            // Act
            await LegacyDocument.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(LegacyHostDocument.GeneratedDocumentContainer.OutputCSharp);
            Assert.Contains("Template", LegacyHostDocument.GeneratedDocumentContainer.OutputCSharp.GeneratedCode, StringComparison.Ordinal);
        }

        [Fact]
        public async Task GetGeneratedOutputAsync_SetsOutputWhenDocumentIsNewer()
        {
            // Arrange
            var newSourceText = SourceText.From("NEW!");
            var newDocumentState = LegacyDocument.State.WithText(newSourceText, Version.GetNewerVersion());
            var newDocument = new DefaultDocumentSnapshot(LegacyDocument.ProjectInternal, newDocumentState);

            // Force the output to be the new output
            await LegacyDocument.GetGeneratedOutputAsync();

            // Act
            await newDocument.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(LegacyHostDocument.GeneratedDocumentContainer.OutputCSharp);
            Assert.Same(newSourceText, LegacyHostDocument.GeneratedDocumentContainer.Source);
            Assert.Equal("NEW!", LegacyHostDocument.GeneratedDocumentContainer.OutputHtml.GeneratedHtml);
        }

        [Fact]
        public async Task GetGeneratedOutputAsync_OnlySetsOutputIfDocumentNewer()
        {
            // Arrange
            var newSourceText = SourceText.From("NEW!");
            var newDocumentState = LegacyDocument.State.WithText(newSourceText, Version.GetNewerVersion());
            var newDocument = new DefaultDocumentSnapshot(LegacyDocument.ProjectInternal, newDocumentState);

            // Force the output to be the new output
            await newDocument.GetGeneratedOutputAsync();

            // Act
            await LegacyDocument.GetGeneratedOutputAsync();

            // Assert
            Assert.NotNull(LegacyHostDocument.GeneratedDocumentContainer.OutputCSharp);
            Assert.Same(newSourceText, LegacyHostDocument.GeneratedDocumentContainer.Source);
            Assert.Equal("NEW!", LegacyHostDocument.GeneratedDocumentContainer.OutputHtml.GeneratedHtml);
        }
    }
}
