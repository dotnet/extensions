// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class DefaultRazorFormattingServiceTest
    {
        [Fact]
        public void MergeEdits_ReturnsSingleEditAsExpected()
        {
            // Arrange
            var source = @"
@code {
public class Foo{}
}
";
            var sourceText = SourceText.From(source);
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = "Bar",
                    Range = new Range(new Position(2, 13), new Position(2, 16))
                },
                new TextEdit()
                {
                    NewText = "    ",
                    Range = new Range(new Position(2, 0), new Position(2, 0))
                },
            };

            // Act
            var collapsedEdit = DefaultRazorFormattingService.MergeEdits(edits, sourceText);

            // Assert
            var multiEditChange = sourceText.WithChanges(edits.Select(e => e.AsTextChange(sourceText)));
            var singleEditChange = sourceText.WithChanges(collapsedEdit.AsTextChange(sourceText));

            Assert.Equal(multiEditChange.ToString(), singleEditChange.ToString());
        }
    }
}
