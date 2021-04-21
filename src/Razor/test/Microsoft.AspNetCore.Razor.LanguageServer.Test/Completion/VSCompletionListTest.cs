// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion
{
    public class VSCompletionListTest
    {
        [Fact]
        public void Convert_CommitCharactersTrue_RemovesCommitCharactersFromItems()
        {
            // Arrange
            var commitCharacters = new Container<string>("<");
            var completionList = new CompletionList(
                new CompletionItem()
                {
                    Label = "Test",
                    CommitCharacters = commitCharacters
                });
            var capabilities = new VSCompletionListCapability()
            {
                CommitCharacters = true,
            };

            // Act
            var vsCompletionList = VSCompletionList.Convert(completionList, capabilities);

            // Assert
            Assert.Collection(vsCompletionList.Items, item => Assert.Null(item.CommitCharacters));
            Assert.Equal(commitCharacters, vsCompletionList.CommitCharacters);
        }

        [Fact]
        public void Convert_CommitCharactersFalse_DoesNotTouchCommitCharacters()
        {
            // Arrange
            var commitCharacters = new Container<string>("<");
            var completionList = new CompletionList(
                new CompletionItem()
                {
                    Label = "Test",
                    CommitCharacters = commitCharacters
                });
            var capabilities = new VSCompletionListCapability()
            {
                CommitCharacters = false,
            };

            // Act
            var vsCompletionList = VSCompletionList.Convert(completionList, capabilities);

            // Assert
            Assert.Collection(vsCompletionList.Items, item => Assert.Equal(commitCharacters, item.CommitCharacters));
            Assert.Null(vsCompletionList.CommitCharacters);
        }
    }
}
