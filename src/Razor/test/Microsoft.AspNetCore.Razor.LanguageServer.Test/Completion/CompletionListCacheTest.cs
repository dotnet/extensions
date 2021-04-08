// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Completion;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    public class CompletionListCacheTest
    {
        private CompletionListCache CompletionListCache { get; } = new CompletionListCache();

        [Fact]
        public void TryGet_SetCompletionList_ReturnsTrue()
        {
            // Arrange
            var completionList = new RazorCompletionItem[1];
            var resultId = CompletionListCache.Set(completionList);

            // Act
            var result = CompletionListCache.TryGet(resultId, out var retrievedCompletionList);

            // Assert
            Assert.True(result);
            Assert.Same(completionList, retrievedCompletionList);
        }

        [Fact]
        public void TryGet_UnknownCompletionList_ReturnsTrue()
        {
            // Act
            var result = CompletionListCache.TryGet(1234, out var retrievedCompletionList);

            // Assert
            Assert.False(result);
            Assert.Null(retrievedCompletionList);
        }

        [Fact]
        public void TryGet_EvictedCompletionList_ReturnsFalse()
        {
            // Arrange
            var initialCompletionList = new RazorCompletionItem[1];
            var initialCompletionListResultId = CompletionListCache.Set(initialCompletionList);
            for (var i = 0; i < CompletionListCache.MaxCacheSize; i++)
            {
                // We now fill the completion list cache up until its cache max so that the initial completion list we set gets evicted.
                CompletionListCache.Set(new RazorCompletionItem[1]);
            }

            // Act
            var result = CompletionListCache.TryGet(initialCompletionListResultId, out var retrievedCompletionList);

            // Assert
            Assert.False(result);
            Assert.Null(retrievedCompletionList);
        }
    }
}
