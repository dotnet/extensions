// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio.LiveShare.Razor.Test;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class GuestProjectPathProviderTest : ForegroundDispatcherTestBase
    {
        public GuestProjectPathProviderTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskFactory = new JoinableTaskFactory(joinableTaskContext.Context);
        }

        public JoinableTaskFactory JoinableTaskFactory { get; }

        [Fact]
        public void TryGetProjectPath_NoTextDocument_ReturnsFalse()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var projectPathProvider = new GuestProjectPathProvider(
                Dispatcher,
                JoinableTaskFactory,
                Mock.Of<ITextDocumentFactoryService>(),
                Mock.Of<ProxyAccessor>(),
                Mock.Of<LiveShareClientProvider>());

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.False(result);
            Assert.Null(filePath);
        }

        [Fact]
        public void TryGetProjectPath_NullHostProjectPath_ReturnsFalse()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var textDocument = Mock.Of<ITextDocument>();
            var textDocumentFactory = Mock.Of<ITextDocumentFactoryService>(factory => factory.TryGetTextDocument(textBuffer, out textDocument) == true);
            var projectPathProvider = new TestGuestProjectPathProvider(
                null,
                Dispatcher,
                JoinableTaskFactory,
                textDocumentFactory,
                Mock.Of<ProxyAccessor>(),
                Mock.Of<LiveShareClientProvider>());

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.False(result);
            Assert.Null(filePath);
        }

        [Fact]
        public async Task TryGetProjectPath_ValidHostProjectPath_ReturnsTrueWithGuestNormalizedPathAsync()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var textDocument = Mock.Of<ITextDocument>();
            var textDocumentFactory = Mock.Of<ITextDocumentFactoryService>(factory => factory.TryGetTextDocument(textBuffer, out textDocument) == true);
            var expectedProjectPath = "/guest/path/project.csproj";

            var collabSession = new TestCollaborationSession(isHost: false);
            var liveShareClientProvider = new LiveShareClientProvider();
            await liveShareClientProvider.CreateServiceAsync(collabSession, CancellationToken.None);

            var projectPathProvider = new TestGuestProjectPathProvider(
                new Uri("vsls:/path/project.csproj"),
                Dispatcher,
                JoinableTaskFactory,
                textDocumentFactory,
                Mock.Of<ProxyAccessor>(),
                liveShareClientProvider);

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedProjectPath, filePath);
        }

        [Fact]
        public async Task GetHostProjectPath_AsksProxyForProjectPathAsync()
        {
            // Arrange
            var expectedGuestFilePath = "/guest/path/index.cshtml";
            var expectedHostFilePath = new Uri("vsls:/path/index.cshtml");
            var expectedHostProjectPath = new Uri("vsls:/path/project.csproj");

            var collabSession = new TestCollaborationSession(isHost: true);
            var liveShareClientProvider = new LiveShareClientProvider();
            await liveShareClientProvider.CreateServiceAsync(collabSession, CancellationToken.None);

            var proxy = Mock.Of<IProjectHierarchyProxy>(p => p.GetProjectPathAsync(expectedHostFilePath, CancellationToken.None) == Task.FromResult(expectedHostProjectPath));
            var proxyAccessor = Mock.Of<ProxyAccessor>(accessor => accessor.GetProjectHierarchyProxy() == proxy);
            var textDocument = Mock.Of<ITextDocument>(document => document.FilePath == expectedGuestFilePath);
            var projectPathProvider = new GuestProjectPathProvider(
                Dispatcher,
                JoinableTaskFactory,
                Mock.Of<ITextDocumentFactoryService>(),
                proxyAccessor,
                liveShareClientProvider);

            // Act
            var hostProjectPath = projectPathProvider.GetHostProjectPath(textDocument);

            // Assert
            Assert.Equal(expectedHostProjectPath, hostProjectPath);
        }

        private class TestGuestProjectPathProvider : GuestProjectPathProvider
        {
            private readonly Uri _hostProjectPath;

            public TestGuestProjectPathProvider(
                Uri hostProjectPath,
                ForegroundDispatcher foregroundDispatcher, 
                JoinableTaskFactory joinableTaskFactory, 
                ITextDocumentFactoryService textDocumentFactory, 
                ProxyAccessor proxyAccessor, 
                LiveShareClientProvider remoteWorkspaceManager) 
                : base(foregroundDispatcher, joinableTaskFactory, textDocumentFactory, proxyAccessor, remoteWorkspaceManager)
            {
                _hostProjectPath = hostProjectPath;
            }

            internal override Uri GetHostProjectPath(ITextDocument textDocument) => _hostProjectPath;
        }
    }
}
