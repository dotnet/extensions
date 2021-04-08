// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DocumentOutputReferenceCapturerTest : LanguageServerTestBase
    {
        public DocumentOutputReferenceCapturerTest()
        {
            ProjectManager = TestProjectSnapshotManager.Create(Dispatcher);
            ProjectManager.AllowNotifyListeners = true;

            HostProject = new HostProject("C:/path/to/project.csproj", RazorConfiguration.Default, "TestNamespace");
            ProjectManager.ProjectAdded(HostProject);
            HostDocument = new HostDocument("C:/path/to/file.razor", "file.razor");
            ProjectManager.DocumentAdded(HostProject, HostDocument, new EmptyTextLoader(HostDocument.FilePath));

            DocumentOutputReferenceCapturer = new DocumentOutputReferenceCapturer();
            DocumentOutputReferenceCapturer.Initialize(ProjectManager);
        }

        private TestProjectSnapshotManager ProjectManager { get; }

        private DocumentOutputReferenceCapturer DocumentOutputReferenceCapturer { get; }

        private HostProject HostProject { get; }

        private HostDocument HostDocument { get; }

        private DocumentSnapshot CurrentDocumentSnapshot => ProjectManager.GetLoadedProject(HostProject.FilePath).GetDocument(HostDocument.FilePath);

        [Fact]
        public async Task DocumentOpened_CapturesOnlyLatestDocumentVersion()
        {
            // Arrange
            var originalDocument = CurrentDocumentSnapshot;
            await originalDocument.GetGeneratedOutputAsync();

            // Act
            ProjectManager.DocumentOpened(HostProject.FilePath, HostDocument.FilePath, SourceText.From(string.Empty));

            await CurrentDocumentSnapshot.GetGeneratedOutputAsync();

            GC.GetTotalMemory(forceFullCollection: true);

            // Assert
            Assert.False(originalDocument.TryGetGeneratedOutput(out _));
            Assert.True(CurrentDocumentSnapshot.TryGetGeneratedOutput(out _));
        }

        [Fact]
        public async Task DocumentClosed_ReleasesAllDocumentVersions()
        {
            // Arrange
            await CurrentDocumentSnapshot.GetGeneratedOutputAsync();

            ProjectManager.DocumentOpened(HostProject.FilePath, HostDocument.FilePath, SourceText.From(string.Empty));

            var originalDocument = CurrentDocumentSnapshot;
            await originalDocument.GetGeneratedOutputAsync();

            // Act
            ProjectManager.DocumentClosed(HostProject.FilePath, HostDocument.FilePath, new EmptyTextLoader(HostDocument.FilePath));

            await CurrentDocumentSnapshot.GetGeneratedOutputAsync();

            GC.GetTotalMemory(forceFullCollection: true);

            // Assert
            Assert.False(originalDocument.TryGetGeneratedOutput(out _));
            Assert.False(CurrentDocumentSnapshot.TryGetGeneratedOutput(out _));
        }

        [Fact]
        public async Task DocumentRemoved_ReleasesAllDocumentVersions()
        {
            // Arrange
            await CurrentDocumentSnapshot.GetGeneratedOutputAsync();

            ProjectManager.DocumentOpened(HostProject.FilePath, HostDocument.FilePath, SourceText.From(string.Empty));

            var originallyPinnedDocument = CurrentDocumentSnapshot;
            await originallyPinnedDocument.GetGeneratedOutputAsync();

            // Act
            ProjectManager.DocumentRemoved(HostProject, HostDocument);

            GC.GetTotalMemory(forceFullCollection: true);

            // Assert
            Assert.False(originallyPinnedDocument.TryGetGeneratedOutput(out _));
        }

        [Fact]
        public async Task ProjectRemoved_ReleasesAllDocumentVersions()
        {
            // Arrange
            await CurrentDocumentSnapshot.GetGeneratedOutputAsync();

            ProjectManager.DocumentOpened(HostProject.FilePath, HostDocument.FilePath, SourceText.From(string.Empty));

            var originallyPinnedDocument = CurrentDocumentSnapshot;
            await originallyPinnedDocument.GetGeneratedOutputAsync();

            // Act
            ProjectManager.ProjectRemoved(HostProject);

            GC.GetTotalMemory(forceFullCollection: true);

            // Assert
            Assert.False(originallyPinnedDocument.TryGetGeneratedOutput(out _));
        }
    }
}
