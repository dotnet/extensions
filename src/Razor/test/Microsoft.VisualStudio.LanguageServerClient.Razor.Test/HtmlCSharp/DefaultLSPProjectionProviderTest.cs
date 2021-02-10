// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class DefaultLSPProjectionProviderTest
    {
        [Fact]
        public async Task GetProjectionAsync_RazorProjection_ReturnsNull()
        {
            // Arrange
            var uri = new Uri("file:///some/folder/to/file.razor");
            var documentSnapshot = new Mock<LSPDocumentSnapshot>(MockBehavior.Strict);
            documentSnapshot.SetupGet(d => d.Uri).Returns(uri);

            var response = new RazorLanguageQueryResponse()
            {
                Kind = RazorLanguageKind.Razor
            };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<RazorLanguageQueryParams, RazorLanguageQueryResponse>(It.IsAny<string>(), RazorLSPConstants.RazorLSPContentTypeName, It.IsAny<RazorLanguageQueryParams>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var documentSynchronizer = new Mock<LSPDocumentSynchronizer>(MockBehavior.Strict);

            var projectionProvider = new DefaultLSPProjectionProvider(requestInvoker.Object, documentSynchronizer.Object, Mock.Of<RazorLogger>(MockBehavior.Strict));

            // Act
            var result = await projectionProvider.GetProjectionAsync(documentSnapshot.Object, new Position(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetProjectionAsync_HtmlProjection_Synchronizes_ReturnsProjection()
        {
            // Arrange
            var uri = new Uri("file:///some/folder/to/file.razor");
            var htmlUri = new Uri("file:///some/folder/to/file.razor__virtual.html");
            var virtualDocumentSnapshot = new HtmlVirtualDocumentSnapshot(htmlUri, Mock.Of<ITextSnapshot>(MockBehavior.Strict), 1);

            var documentSnapshotObj = new Mock<LSPDocumentSnapshot>(MockBehavior.Strict);
            documentSnapshotObj.SetupGet(d => d.Uri).Returns(uri);
            documentSnapshotObj.SetupGet(d => d.Version).Returns(1);
            documentSnapshotObj.SetupGet(d => d.VirtualDocuments).Returns(new[] { virtualDocumentSnapshot });
            var documentSnapshot = documentSnapshotObj.Object;

            var expectedPosition = new Position(0, 0);
            var response = new RazorLanguageQueryResponse()
            {
                Kind = RazorLanguageKind.Html,
                HostDocumentVersion = 1,
                Position = new Position(expectedPosition.Line, expectedPosition.Character)
            };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<RazorLanguageQueryParams, RazorLanguageQueryResponse>(It.IsAny<string>(), RazorLSPConstants.RazorLSPContentTypeName, It.IsAny<RazorLanguageQueryParams>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var documentSynchronizer = new Mock<LSPDocumentSynchronizer>(MockBehavior.Strict);
            documentSynchronizer
                .Setup(d => d.TrySynchronizeVirtualDocumentAsync(documentSnapshot.Version, virtualDocumentSnapshot, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true));

            var projectionProvider = new DefaultLSPProjectionProvider(requestInvoker.Object, documentSynchronizer.Object, Mock.Of<RazorLogger>(MockBehavior.Strict));

            // Act
            var result = await projectionProvider.GetProjectionAsync(documentSnapshot, new Position(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(htmlUri, result.Uri);
            Assert.Equal(RazorLanguageKind.Html, result.LanguageKind);
            Assert.Equal(expectedPosition, result.Position);
        }

        [Fact]
        public async Task GetProjectionAsync_CSharpProjection_Synchronizes_ReturnsProjection()
        {
            // Arrange
            var uri = new Uri("file:///some/folder/to/file.razor");
            var csharpUri = new Uri("file:///some/folder/to/file.razor__virtual.cs");
            var virtualDocumentSnapshot = new CSharpVirtualDocumentSnapshot(csharpUri, Mock.Of<ITextSnapshot>(MockBehavior.Strict), 1);

            var documentSnapshotObj = new Mock<LSPDocumentSnapshot>(MockBehavior.Strict);
            documentSnapshotObj.SetupGet(d => d.Uri).Returns(uri);
            documentSnapshotObj.SetupGet(d => d.Version).Returns(1);
            documentSnapshotObj.SetupGet(d => d.VirtualDocuments).Returns(new[] { virtualDocumentSnapshot });
            var documentSnapshot = documentSnapshotObj.Object;

            var expectedPosition = new Position(0, 0);
            var response = new RazorLanguageQueryResponse()
            {
                Kind = RazorLanguageKind.CSharp,
                HostDocumentVersion = 1,
                Position = new Position(expectedPosition.Line, expectedPosition.Character)
            };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<RazorLanguageQueryParams, RazorLanguageQueryResponse>(It.IsAny<string>(), RazorLSPConstants.RazorLSPContentTypeName, It.IsAny<RazorLanguageQueryParams>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var documentSynchronizer = new Mock<LSPDocumentSynchronizer>(MockBehavior.Strict);
            documentSynchronizer
                .Setup(d => d.TrySynchronizeVirtualDocumentAsync(documentSnapshot.Version, virtualDocumentSnapshot, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true));

            var projectionProvider = new DefaultLSPProjectionProvider(requestInvoker.Object, documentSynchronizer.Object, Mock.Of<RazorLogger>(MockBehavior.Strict));

            // Act
            var result = await projectionProvider.GetProjectionAsync(documentSnapshot, new Position(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(csharpUri, result.Uri);
            Assert.Equal(RazorLanguageKind.CSharp, result.LanguageKind);
            Assert.Equal(expectedPosition, result.Position);
        }

        [Fact]
        public async Task GetProjectionAsync_UndefinedHostDocumentVersionResponse_ReturnsProjection()
        {
            // Arrange
            var uri = new Uri("file:///some/folder/to/file.razor");
            var htmlUri = new Uri("file:///some/folder/to/file.razor__virtual.html");
            var virtualDocumentSnapshot = new HtmlVirtualDocumentSnapshot(htmlUri, Mock.Of<ITextSnapshot>(MockBehavior.Strict), 1);

            var documentSnapshotObj = new Mock<LSPDocumentSnapshot>(MockBehavior.Strict);
            documentSnapshotObj.SetupGet(d => d.Uri).Returns(uri);
            documentSnapshotObj.SetupGet(d => d.Version).Returns(1);
            documentSnapshotObj.SetupGet(d => d.VirtualDocuments).Returns(new[] { virtualDocumentSnapshot });
            var documentSnapshot = documentSnapshotObj.Object;

            var expectedPosition = new Position(0, 0);
            var response = new RazorLanguageQueryResponse()
            {
                Kind = RazorLanguageKind.Html,
                HostDocumentVersion = null,
                Position = new Position(expectedPosition.Line, expectedPosition.Character)
            };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<RazorLanguageQueryParams, RazorLanguageQueryResponse>(It.IsAny<string>(), RazorLSPConstants.RazorLSPContentTypeName, It.IsAny<RazorLanguageQueryParams>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var documentSynchronizer = new Mock<LSPDocumentSynchronizer>(MockBehavior.Strict);
            documentSynchronizer
                .Setup(d => d.TrySynchronizeVirtualDocumentAsync(documentSnapshot.Version, virtualDocumentSnapshot, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true));

            var logger = new Mock<RazorLogger>(MockBehavior.Strict);
            logger.Setup(l => l.LogVerbose(It.IsAny<string>())).Verifiable();
            var projectionProvider = new DefaultLSPProjectionProvider(requestInvoker.Object, documentSynchronizer.Object, logger.Object);

            // Act
            var result = await projectionProvider.GetProjectionAsync(documentSnapshot, new Position(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(htmlUri, result.Uri);
            Assert.Equal(RazorLanguageKind.Html, result.LanguageKind);
            Assert.Equal(expectedPosition, result.Position);
        }

        [Fact]
        public async Task GetProjectionAsync_SynchronizationFails_ReturnsNull()
        {
            // Arrange
            var uri = new Uri("file:///some/folder/to/file.razor");
            var csharpUri = new Uri("file:///some/folder/to/file.razor__virtual.cs");
            var virtualDocumentSnapshot = new CSharpVirtualDocumentSnapshot(csharpUri, Mock.Of<ITextSnapshot>(MockBehavior.Strict), 1);

            var documentSnapshotObj = new Mock<LSPDocumentSnapshot>(MockBehavior.Strict);
            documentSnapshotObj.SetupGet(d => d.Uri).Returns(uri);
            documentSnapshotObj.SetupGet(d => d.Version).Returns(1);
            documentSnapshotObj.SetupGet(d => d.VirtualDocuments).Returns(new[] { virtualDocumentSnapshot });
            var documentSnapshot = documentSnapshotObj.Object;

            var expectedPosition = new Position(0, 0);
            var response = new RazorLanguageQueryResponse()
            {
                Kind = RazorLanguageKind.CSharp,
                HostDocumentVersion = 1,
                Position = new Position(expectedPosition.Line, expectedPosition.Character)
            };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<RazorLanguageQueryParams, RazorLanguageQueryResponse>(It.IsAny<string>(), RazorLSPConstants.RazorLSPContentTypeName, It.IsAny<RazorLanguageQueryParams>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var documentSynchronizer = new Mock<LSPDocumentSynchronizer>(MockBehavior.Strict);
            documentSynchronizer
                .Setup(d => d.TrySynchronizeVirtualDocumentAsync(documentSnapshot.Version, virtualDocumentSnapshot, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(false));

            var projectionProvider = new DefaultLSPProjectionProvider(requestInvoker.Object, documentSynchronizer.Object, Mock.Of<RazorLogger>(MockBehavior.Strict));

            // Act
            var result = await projectionProvider.GetProjectionAsync(documentSnapshot, new Position(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }
    }
}
