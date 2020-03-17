// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class DefaultLSPRequestInvokerTest
    {
        [Fact]
        public async Task RequestServerAsync_InvokesRazorLanguageClient()
        {
            // Arrange
            var called = false;
            var expectedMethod = "razor/test";
            var broker = new TestLanguageClientBroker((contentType, method) =>
            {
                called = true;
                Assert.Equal(RazorLSPContentTypeDefinition.Name, contentType);
                Assert.Equal(expectedMethod, method);
            });
            var requestInvoker = new DefaultLSPRequestInvoker(broker);

            // Act
            await requestInvoker.RequestServerAsync<object, object>(expectedMethod, LanguageServerKind.Razor, new object(), CancellationToken.None);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public async Task RequestServerAsync_InvokesHtmlLanguageClient()
        {
            // Arrange
            var called = false;
            var expectedMethod = "textDocument/test";
            var broker = new TestLanguageClientBroker((contentType, method) =>
            {
                called = true;
                Assert.Equal(HtmlVirtualDocumentFactory.HtmlLSPContentTypeName, contentType);
                Assert.Equal(expectedMethod, method);
            });
            var requestInvoker = new DefaultLSPRequestInvoker(broker);

            // Act
            await requestInvoker.RequestServerAsync<object, object>(expectedMethod, LanguageServerKind.Html, new object(), CancellationToken.None);

            // Assert
            Assert.True(called);
        }

        [Fact]
        public async Task RequestServerAsync_InvokesCSharpLanguageClient()
        {
            // Arrange
            var called = false;
            var expectedMethod = "textDocument/test";
            var broker = new TestLanguageClientBroker((contentType, method) =>
            {
                called = true;
                Assert.Equal(CSharpVirtualDocumentFactory.CSharpLSPContentTypeName, contentType);
                Assert.Equal(expectedMethod, method);
            });
            var requestInvoker = new DefaultLSPRequestInvoker(broker);

            // Act
            await requestInvoker.RequestServerAsync<object, object>(expectedMethod, LanguageServerKind.CSharp, new object(), CancellationToken.None);

            // Assert
            Assert.True(called);
        }

        private class TestLanguageClientBroker : ILanguageClientBroker
        {
            private readonly Action<string, string> _callback;

            public TestLanguageClientBroker(Action<string, string> callback)
            {
                _callback = callback;
            }

            public Task LoadAsync(ILanguageClientMetadata metadata, ILanguageClient client)
            {
                throw new NotImplementedException();
            }

            public Task<(ILanguageClient, JToken)> RequestAsync(
                string[] contentTypes,
                Func<JToken, bool> capabilitiesFilter,
                string method,
                JToken parameters,
                CancellationToken cancellationToken)
            {
                // We except it to be called with only one content type.
                var contentType = Assert.Single(contentTypes);

                _callback?.Invoke(contentType, method);

                return Task.FromResult<(ILanguageClient, JToken)>((null, null));
            }
        }
    }
}
