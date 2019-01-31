// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LiveShare.Razor.Test;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LiveShare.Razor.Guest
{
    public class GuestProjectPathProviderTest
    {
        public GuestProjectPathProviderTest()
        {
            JoinableTaskContext = new JoinableTaskContext();
            var collabSession = new TestCollaborationSession(isHost: false);
            SessionAccessor = Mock.Of<LiveShareSessionAccessor>(accessor => accessor.IsGuestSessionActive == true && accessor.Session == collabSession);
        }

        private JoinableTaskContext JoinableTaskContext { get; }

        private LiveShareSessionAccessor SessionAccessor { get; }

        [Fact]
        public void TryGetProjectPath_GuestSessionNotActive_ReturnsFalse()
        {
            // Arrange
            var sessionAccessor = Mock.Of<LiveShareSessionAccessor>(accessor => accessor.IsGuestSessionActive == false);
            var textBuffer = Mock.Of<ITextBuffer>();
            var textDocument = Mock.Of<ITextDocument>();
            var textDocumentFactory = Mock.Of<ITextDocumentFactoryService>(factory => factory.TryGetTextDocument(textBuffer, out textDocument) == true);
            var projectPathProvider = new TestGuestProjectPathProvider(
                new Uri("vsls:/path/project.csproj"),
                JoinableTaskContext,
                textDocumentFactory,
                Mock.Of<ProxyAccessor>(),
                sessionAccessor);

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.False(result);
            Assert.Null(filePath);
        }

        [Fact]
        public void TryGetProjectPath_NoTextDocument_ReturnsFalse()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var projectPathProvider = new GuestProjectPathProvider(
                JoinableTaskContext,
                Mock.Of<ITextDocumentFactoryService>(),
                Mock.Of<ProxyAccessor>(),
                SessionAccessor);

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
                JoinableTaskContext,
                textDocumentFactory,
                Mock.Of<ProxyAccessor>(),
                SessionAccessor);

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.False(result);
            Assert.Null(filePath);
        }

        [Fact]
        public void TryGetProjectPath_ValidHostProjectPath_ReturnsTrueWithGuestNormalizedPath()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>();
            var textDocument = Mock.Of<ITextDocument>();
            var textDocumentFactory = Mock.Of<ITextDocumentFactoryService>(factory => factory.TryGetTextDocument(textBuffer, out textDocument) == true);
            var expectedProjectPath = "/guest/path/project.csproj";
            var projectPathProvider = new TestGuestProjectPathProvider(
                new Uri("vsls:/path/project.csproj"),
                JoinableTaskContext,
                textDocumentFactory,
                Mock.Of<ProxyAccessor>(),
                SessionAccessor);

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedProjectPath, filePath);
        }

        [Fact]
        public void GetHostProjectPath_AsksProxyForProjectPathAsync()
        {
            // Arrange
            var expectedGuestFilePath = "/guest/path/index.cshtml";
            var expectedHostFilePath = new Uri("vsls:/path/index.cshtml");
            var expectedHostProjectPath = new Uri("vsls:/path/project.csproj");
            var collabSession = new TestCollaborationSession(isHost: true);
            var sessionAccessor = Mock.Of<LiveShareSessionAccessor>(accessor => accessor.IsGuestSessionActive == true && accessor.Session == collabSession);

            var proxy = Mock.Of<IProjectHierarchyProxy>(p => p.GetProjectPathAsync(expectedHostFilePath, CancellationToken.None) == Task.FromResult(expectedHostProjectPath));
            var proxyAccessor = Mock.Of<ProxyAccessor>(accessor => accessor.GetProjectHierarchyProxy() == proxy);
            var textDocument = Mock.Of<ITextDocument>(document => document.FilePath == expectedGuestFilePath);
            var projectPathProvider = new GuestProjectPathProvider(
                JoinableTaskContext,
                Mock.Of<ITextDocumentFactoryService>(),
                proxyAccessor,
                sessionAccessor);

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
                JoinableTaskContext joinableTaskContext,
                ITextDocumentFactoryService textDocumentFactory,
                ProxyAccessor proxyAccessor,
                LiveShareSessionAccessor liveShareSessionAccessor)
                : base(joinableTaskContext, textDocumentFactory, proxyAccessor, liveShareSessionAccessor)
            {
                _hostProjectPath = hostProjectPath;
            }

            internal override Uri GetHostProjectPath(ITextDocument textDocument) => _hostProjectPath;
        }
    }
}
