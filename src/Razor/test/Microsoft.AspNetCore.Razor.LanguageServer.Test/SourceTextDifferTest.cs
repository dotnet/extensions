// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class SourceTextDifferTest
    {
        [Theory]
        [InlineData("asdf", ";lkj")]
        [InlineData("asdf", ";asd")]
        [InlineData("", "")]
        [InlineData("", "a")]
        [InlineData("a", "b")]
        [InlineData("a", "a")]
        [InlineData("a", "")]
        [InlineData("aabd", "abc")]
        [InlineData("aabd", "a")]
        [InlineData("aabd", "h")]
        [InlineData("aabd", "trtrt45rtt()")]
        [InlineData("trtrt4 5rtt()", "atbd")]
        [InlineData(@"trtrt4\n5rtt()", "atb\nd")]
        [InlineData(@"Hello\r\nWorld\r\n123", "Hola\r\nWorld\r\n\r\n1234")]
        public void GetMinimalTextChanges_ReturnsAccurateResults(string oldStr, string newStr)
        {
            // Arrange
            var oldText = SourceText.From(oldStr);
            var newText = SourceText.From(newStr);

            // Act 1
            var characterChanges = SourceTextDiffer.GetMinimalTextChanges(oldText, newText, lineDiffOnly: false);

            // Assert 1
            var changedText = oldText.WithChanges(characterChanges);
            Assert.Equal(newStr, changedText.ToString());

            // Act 2
            var lineChanges = SourceTextDiffer.GetMinimalTextChanges(oldText, newText, lineDiffOnly: false);

            // Assert 2
            changedText = oldText.WithChanges(lineChanges);
            Assert.Equal(newStr, changedText.ToString());
        }

        [Fact]
        public void GetMinimalTextChanges_ReturnsExpectedResults()
        {
            // Arrange
            var oldText = SourceText.From(@"
<div>
  Hello!
</div>
".Replace(Environment.NewLine, "\r\n", StringComparison.Ordinal));
            var newText = SourceText.From(@"
<div>
  Hola!
</div>".Replace(Environment.NewLine, "\r\n", StringComparison.Ordinal));

            // Act 1
            var characterChanges = SourceTextDiffer.GetMinimalTextChanges(oldText, newText, lineDiffOnly: false);

            // Assert 1
            Assert.Collection(characterChanges,
                change => Assert.Equal(new TextChange(TextSpan.FromBounds(12, 13), "o"), change),
                change => Assert.Equal(new TextChange(TextSpan.FromBounds(14, 16), "a"), change),
                change => Assert.Equal(new TextChange(TextSpan.FromBounds(25, 27), string.Empty), change));

            // Act 2
            var lineChanges = SourceTextDiffer.GetMinimalTextChanges(oldText, newText, lineDiffOnly: true);

            // Assert 2
            var change = Assert.Single(lineChanges);
            Assert.Equal(new TextChange(TextSpan.FromBounds(9, 27), "  Hola!\r\n</div>"), change);
        }
    }
}
