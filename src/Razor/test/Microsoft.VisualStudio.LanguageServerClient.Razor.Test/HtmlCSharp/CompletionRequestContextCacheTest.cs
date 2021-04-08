// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class CompletionRequestContextCacheTest
    {
        private Uri HostDocumentUri { get; } = new Uri("C:/path/to/file.razor");

        private Uri ProjectedUri { get; } = new Uri("C:/path/to/file.foo");

        private LanguageServerKind LanguageServerKind { get; } = LanguageServerKind.CSharp;

        private CompletionRequestContextCache Cache { get; } = new CompletionRequestContextCache();

        [Fact]
        public void TryGet_SetRequestContext_ReturnsTrue()
        {
            // Arrange
            var requestContext = new CompletionRequestContext(HostDocumentUri, ProjectedUri, LanguageServerKind);
            var resultId = Cache.Set(requestContext);

            // Act
            var result = Cache.TryGet(resultId, out var retrievedRequestContext);

            // Assert
            Assert.True(result);
            Assert.Same(requestContext, retrievedRequestContext);
        }

        [Fact]
        public void TryGet_UnknownRequestContext_ReturnsTrue()
        {
            // Act
            var result = Cache.TryGet(1234, out var retrievedRequestContext);

            // Assert
            Assert.False(result);
            Assert.Null(retrievedRequestContext);
        }

        [Fact]
        public void TryGet_EvictedCompletionList_ReturnsFalse()
        {
            // Arrange
            var initialRequestContext = new CompletionRequestContext(HostDocumentUri, ProjectedUri, LanguageServerKind);
            var initialRequestContextId = Cache.Set(initialRequestContext);
            for (var i = 0; i < CompletionRequestContextCache.MaxCacheSize; i++)
            {
                // We now fill the completion list cache up until its cache max so that the initial completion list we set gets evicted.
                Cache.Set(new CompletionRequestContext(HostDocumentUri, ProjectedUri, LanguageServerKind));
            }

            // Act
            var result = Cache.TryGet(initialRequestContextId, out var retrievedRequestContext);

            // Assert
            Assert.False(result);
            Assert.Null(retrievedRequestContext);
        }
    }
}
