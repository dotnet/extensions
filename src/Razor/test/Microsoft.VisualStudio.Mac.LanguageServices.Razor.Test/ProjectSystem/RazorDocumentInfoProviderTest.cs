// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.Razor;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class RazorDocumentInfoProviderTest : WorkspaceTestBase
    {
        public RazorDocumentInfoProviderTest()
        {
            var serviceProviderFactory = new DefaultRazorDocumentServiceProviderFactory();
            var lspEditorEnabledFeatureDetector = Mock.Of<LSPEditorFeatureDetector>(detector => detector.IsLSPEditorFeatureEnabled() == true, MockBehavior.Strict);
            InnerDynamicDocumentInfoProvider = new DefaultRazorDynamicFileInfoProvider(serviceProviderFactory, lspEditorEnabledFeatureDetector);
            ProjectSnapshotManager = new TestProjectSnapshotManager(Workspace);

            var hostProject = new HostProject("C:/path/to/project.csproj", RazorConfiguration.Default, "RootNamespace");
            ProjectSnapshotManager.ProjectAdded(hostProject);

            var hostDocument = new HostDocument("C:/path/to/document.cshtml", "/C:/path/to/document.cshtml");
            var sourceText = SourceText.From("Hello World");
            var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default, hostDocument.FilePath);
            ProjectSnapshotManager.DocumentAdded(hostProject, hostDocument, TextLoader.From(textAndVersion));

            ProjectSnapshot = ProjectSnapshotManager.Projects[0];
            DocumentSnapshot = ProjectSnapshot.GetDocument(hostDocument.FilePath);
            var factory = new Mock<VisualStudioMacDocumentInfoFactory>(MockBehavior.Strict);
            factory.Setup(f => f.CreateEmpty(It.IsAny<string>(), It.IsAny<ProjectId>()))
                .Returns<string, ProjectId>((razorFilePath, projectId) =>
                {
                    var documentId = DocumentId.CreateNewId(projectId);
                    var documentInfo = DocumentInfo.Create(documentId, "testDoc", filePath: razorFilePath);
                    return documentInfo;
                });
            Factory = factory.Object;
        }

        private VisualStudioMacDocumentInfoFactory Factory { get; }

        private DefaultRazorDynamicFileInfoProvider InnerDynamicDocumentInfoProvider { get; }

        private TestProjectSnapshotManager ProjectSnapshotManager { get; }

        private ProjectSnapshot ProjectSnapshot { get; }

        private DocumentSnapshot DocumentSnapshot { get; }

        [Fact]
        public void DelegatedUpdateFileInfo_UnknownDocument_Noops()
        {
            // Arrange
            var provider = new RazorDynamicDocumentInfoProvider(Factory, InnerDynamicDocumentInfoProvider);
            provider.Updated += (_) => throw new XunitException("This should not have been called.");
            var documentContainer = new DefaultDynamicDocumentContainer(DocumentSnapshot);

            // Act & Assert
            InnerDynamicDocumentInfoProvider.UpdateFileInfo(ProjectSnapshot.FilePath, documentContainer);
        }

        [Fact]
        public void DelegatedUpdateFileInfo_KnownDocument_TriggersUpdate()
        {
            // Arrange
            var provider = new RazorDynamicDocumentInfoProvider(Factory, InnerDynamicDocumentInfoProvider);
            DocumentInfo documentInfo = null;
            provider.Updated += (info) => documentInfo = info;

            // Populate the providers understanding of our project/document
            provider.GetDynamicDocumentInfo(ProjectId.CreateNewId(), ProjectSnapshot.FilePath, DocumentSnapshot.FilePath);
            var documentContainer = new DefaultDynamicDocumentContainer(DocumentSnapshot);

            // Act
            InnerDynamicDocumentInfoProvider.UpdateFileInfo(ProjectSnapshot.FilePath, documentContainer);

            // Assert
            Assert.NotNull(documentInfo);
            Assert.Equal(DocumentSnapshot.FilePath, documentInfo.FilePath);
        }

        [Fact]
        public void DelegatedSuppressDocument_UnknownDocument_Noops()
        {
            // Arrange
            var provider = new RazorDynamicDocumentInfoProvider(Factory, InnerDynamicDocumentInfoProvider);
            provider.Updated += (_) => throw new XunitException("This should not have been called.");
            var documentContainer = new DefaultDynamicDocumentContainer(DocumentSnapshot);

            // Act & Assert
            InnerDynamicDocumentInfoProvider.SuppressDocument(ProjectSnapshot.FilePath, DocumentSnapshot.FilePath);
        }

        [Fact]
        public void DelegatedSuppressDocument_KnownDocument_NotUpdated_Noops()
        {
            // Arrange
            var provider = new RazorDynamicDocumentInfoProvider(Factory, InnerDynamicDocumentInfoProvider);
            provider.Updated += (_) => throw new XunitException("This should not have been called.");

            // Populate the providers understanding of our project/document
            provider.GetDynamicDocumentInfo(ProjectId.CreateNewId(), ProjectSnapshot.FilePath, DocumentSnapshot.FilePath);
            var documentContainer = new DefaultDynamicDocumentContainer(DocumentSnapshot);

            // Act & Assert
            InnerDynamicDocumentInfoProvider.SuppressDocument(ProjectSnapshot.FilePath, DocumentSnapshot.FilePath);
        }

        [Fact]
        public void DelegatedSuppressDocument_KnownAndUpdatedDocument_TriggersUpdate()
        {
            // Arrange
            var provider = new RazorDynamicDocumentInfoProvider(Factory, InnerDynamicDocumentInfoProvider);
            DocumentInfo documentInfo = null;
            provider.Updated += (info) => documentInfo = info;

            // Populate the providers understanding of our project/document
            provider.GetDynamicDocumentInfo(ProjectId.CreateNewId(), ProjectSnapshot.FilePath, DocumentSnapshot.FilePath);
            var documentContainer = new DefaultDynamicDocumentContainer(DocumentSnapshot);

            // Update the document with content
            InnerDynamicDocumentInfoProvider.UpdateFileInfo(ProjectSnapshot.FilePath, documentContainer);

            // Act
            InnerDynamicDocumentInfoProvider.SuppressDocument(ProjectSnapshot.FilePath, DocumentSnapshot.FilePath);

            // Assert
            Assert.NotNull(documentInfo);
            Assert.Equal(DocumentSnapshot.FilePath, documentInfo.FilePath);
        }

        [Fact]
        public void DelegatedRemoveDynamicDocumentInfo_UntracksDocument()
        {
            // Arrange
            var provider = new RazorDynamicDocumentInfoProvider(Factory, InnerDynamicDocumentInfoProvider);

            // Populate the providers understanding of our project/document
            provider.GetDynamicDocumentInfo(ProjectId.CreateNewId(), ProjectSnapshot.FilePath, DocumentSnapshot.FilePath);
            var documentContainer = new DefaultDynamicDocumentContainer(DocumentSnapshot);

            // Update the document with content
            InnerDynamicDocumentInfoProvider.UpdateFileInfo(ProjectSnapshot.FilePath, documentContainer);

            // Now explode if any further updates happen
            provider.Updated += (_) => throw new XunitException("This should not have been called.");

            // Act
            provider.RemoveDynamicDocumentInfo(ProjectId.CreateNewId(), ProjectSnapshot.FilePath, DocumentSnapshot.FilePath);

            // Assert this should not update
            InnerDynamicDocumentInfoProvider.UpdateFileInfo(ProjectSnapshot.FilePath, documentContainer);
        }
    }
}
