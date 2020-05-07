// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class LSPRazorProjectHostTest
    {
        public LSPRazorProjectHostTest()
        {
            var joinableTaskContext = new JoinableTaskContextNode(new JoinableTaskContext());
            JoinableTaskContext = joinableTaskContext.Context;
        }

        private JoinableTaskContext JoinableTaskContext { get; }

        [Fact]
        public async Task LoadAsync_NoopsWhenLSPEditorFeatureNotAvailable()
        {
            // Arrange
            var featureDetector = Mock.Of<LSPEditorFeatureDetector>(f => f.IsLSPEditorFeatureEnabled() == false);
            var broker = new Lazy<ILanguageClientBroker>();
            var client = new Lazy<ILanguageClient, IDictionary<string, object>>(
                () => throw new NotImplementedException(), new Dictionary<string, object>());
            var languageClients = new[] { client };

            var host = new LSPRazorProjectHost(JoinableTaskContext, featureDetector, broker, languageClients);

            // Act & Assert (does not throw)
            await host.LoadAsync().ConfigureAwait(false);
        }

        [Fact]
        public async Task LoadAsync_LSPEditorFeatureAvailable_LoadsClients()
        {
            // Arrange
            var featureDetector = Mock.Of<LSPEditorFeatureDetector>(f => f.IsLSPEditorFeatureEnabled() == true);

            var metadata = new Dictionary<string, object>()
            {
                { "ClientName", "RazorClient" },
                { "ContentTypes", new[] { "RazorLSP" } }
            };
            var client = new Lazy<ILanguageClient, IDictionary<string, object>>(() => Mock.Of<ILanguageClient>(), metadata);
            var languageClients = new[] { client };

            var loaded = false;
            var brokerMock = new Mock<ILanguageClientBroker>();
            brokerMock
                .Setup(b => b.LoadAsync(It.IsAny<ILanguageClientMetadata>(), It.IsAny<ILanguageClient>()))
                .Returns(Task.CompletedTask)
                .Callback<ILanguageClientMetadata, ILanguageClient>((metadata, c) =>
                {
                    loaded = true;
                    Assert.Same(client.Value, c);
                    Assert.Equal("RazorClient", metadata.ClientName);
                    var contentType = Assert.Single(metadata.ContentTypes);
                    Assert.Equal("RazorLSP", contentType);
                });
            var broker = new Lazy<ILanguageClientBroker>(() => brokerMock.Object);

            var host = new LSPRazorProjectHost(JoinableTaskContext, featureDetector, broker, languageClients);

            // Act
            await host.LoadAsync().ConfigureAwait(false);

            // Assert
            Assert.True(loaded);
        }
    }
}
