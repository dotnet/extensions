// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class ProjectStateGeneratedOutputTest : WorkspaceTestBase
    {
        public ProjectStateGeneratedOutputTest()
        {
            HostProject = new HostProject(TestProjectData.SomeProject.FilePath, FallbackRazorConfiguration.MVC_2_0, TestProjectData.SomeProject.RootNamespace);
            HostProjectWithConfigurationChange = new HostProject(TestProjectData.SomeProject.FilePath, FallbackRazorConfiguration.MVC_1_0, TestProjectData.SomeProject.RootNamespace);

            SomeTagHelpers = new List<TagHelperDescriptor>();
            SomeTagHelpers.Add(TagHelperDescriptorBuilder.Create("Test1", "TestAssembly").Build());

            HostDocument = TestProjectData.SomeProjectFile1;

            Text = SourceText.From("Hello, world!");
            TextLoader = () => Task.FromResult(TextAndVersion.Create(Text, VersionStamp.Create()));
        }

        private HostDocument HostDocument { get; }

        private HostProject HostProject { get; }

        private HostProject HostProjectWithConfigurationChange { get; }

        private TestTagHelperResolver TagHelperResolver { get; } = new TestTagHelperResolver();

        private List<TagHelperDescriptor> SomeTagHelpers { get; }

        private Func<Task<TextAndVersion>> TextLoader { get; }

        private SourceText Text { get; }

        protected override void ConfigureWorkspaceServices(List<IWorkspaceService> services)
        {
            services.Add(TagHelperResolver);
        }

        protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
        {
            builder.SetImportFeature(new TestImportProjectFeature());
        }

        [Fact]
        public async Task HostDocumentAdded_CachesOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithAddedHostDocument(TestProjectData.AnotherProjectFile1, DocumentState.EmptyLoader);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.Same(originalOutput, actualOutput);
            Assert.Equal(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.NotEqual(state.ProjectWorkspaceStateVersion, actualOutputVersion);
            Assert.NotEqual(state.ConfigurationVersion, actualOutputVersion);
        }

        [Fact]
        public async Task HostDocumentAdded_Import_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(state.DocumentCollectionVersion, actualInputVersion);
        }

        [Fact]
        public async Task HostDocumentChanged_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader)
                .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var version = VersionStamp.Create();
            var state = original.WithChangedHostDocument(HostDocument, () =>
            {
                return Task.FromResult(TextAndVersion.Create(SourceText.From("@using System"), version));
            });

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.NotEqual(originalOutputVersion, actualOutputVersion);
            Assert.Equal(version, actualInputVersion);
        }

        [Fact]
        public async Task HostDocumentChanged_Import_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader)
                .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var version = VersionStamp.Create();
            var state = original.WithChangedHostDocument(TestProjectData.SomeProjectImportFile, () =>
            {
                return Task.FromResult(TextAndVersion.Create(SourceText.From("@using System"), version));
            });

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.NotEqual(originalOutputVersion, actualOutputVersion);
            Assert.Equal(version, actualInputVersion);
        }

        [Fact]
        public async Task HostDocumentRemoved_Import_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader)
                .WithAddedHostDocument(TestProjectData.SomeProjectImportFile, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithRemovedHostDocument(TestProjectData.SomeProjectImportFile);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(state.DocumentCollectionVersion, actualInputVersion);
        }

        [Fact]
        public async Task ProjectWorkspaceStateChange_CachesOutput_EvenWhenNewerProjectWorkspaceState()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader)
                .WithProjectWorkspaceState(ProjectWorkspaceState.Default);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);
            var changed = new ProjectWorkspaceState(Array.Empty<TagHelperDescriptor>(), default);

            // Act
            var state = original.WithProjectWorkspaceState(changed);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.Same(originalOutput, actualOutput);
            Assert.Equal(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(state.ProjectWorkspaceStateVersion, actualInputVersion);
        }

        // The generated code's text doesn't change as a result, so the output version does not change
        [Fact]
        public async Task ProjectWorkspaceStateChange_WithTagHelperChange_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);
            var changed = new ProjectWorkspaceState(SomeTagHelpers, default);

            // Act
            var state = original.WithProjectWorkspaceState(changed);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.Equal(originalOutputVersion, actualOutputVersion);
            Assert.Equal(state.ProjectWorkspaceStateVersion, actualInputVersion);
        }

        [Fact]
        public async Task ProjectWorkspaceStateChange_WithProjectWorkspaceState_CSharpLanguageVersionChange_DoesNotCacheOutput()
        {
            // Arrange
            var csharp8ValidConfiguration = RazorConfiguration.Create(RazorLanguageVersion.Version_3_0, HostProject.Configuration.ConfigurationName, HostProject.Configuration.Extensions);
            var hostProject = new HostProject(TestProjectData.SomeProject.FilePath, csharp8ValidConfiguration, TestProjectData.SomeProject.RootNamespace);
            var originalWorkspaceState = new ProjectWorkspaceState(SomeTagHelpers, default);
            var original =
                ProjectState.Create(Workspace.Services, hostProject, originalWorkspaceState)
                .WithAddedHostDocument(HostDocument, () =>
                {
                    return Task.FromResult(TextAndVersion.Create(SourceText.From("@DateTime.Now"), VersionStamp.Default));
                });
            var changedWorkspaceState = new ProjectWorkspaceState(SomeTagHelpers, LanguageVersion.CSharp8);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithProjectWorkspaceState(changedWorkspaceState);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.NotEqual(originalOutputVersion, actualOutputVersion);
            Assert.Equal(state.ProjectWorkspaceStateVersion, actualInputVersion);
        }

        [Fact]
        public async Task ConfigurationChange_DoesNotCacheOutput()
        {
            // Arrange
            var original =
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, DocumentState.EmptyLoader);

            var (originalOutput, originalInputVersion, originalOutputVersion) = await GetOutputAsync(original, HostDocument);

            // Act
            var state = original.WithHostProject(HostProjectWithConfigurationChange);

            // Assert
            var (actualOutput, actualInputVersion, actualOutputVersion) = await GetOutputAsync(state, HostDocument);
            Assert.NotSame(originalOutput, actualOutput);
            Assert.NotEqual(originalInputVersion, actualInputVersion);
            Assert.NotEqual(originalOutputVersion, actualOutputVersion);
            Assert.NotEqual(state.ProjectWorkspaceStateVersion, actualInputVersion);
        }

        private static Task<(RazorCodeDocument, VersionStamp, VersionStamp)> GetOutputAsync(ProjectState project, HostDocument hostDocument)
        {
            var document = project.Documents[hostDocument.FilePath];
            return GetOutputAsync(project, document);
        }

        private static Task<(RazorCodeDocument, VersionStamp, VersionStamp)> GetOutputAsync(ProjectState project, DocumentState document)
        {

            var projectSnapshot = new DefaultProjectSnapshot(project);
            var documentSnapshot = new DefaultDocumentSnapshot(projectSnapshot, document);
            return document.GetGeneratedOutputAndVersionAsync(projectSnapshot, documentSnapshot);
        }
    }
}
