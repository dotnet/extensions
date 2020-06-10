// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class DefaultLSPEditorServiceTest
    {
        [Fact]
        public void ExtractsCursorPlaceholder_AppliesEditsCorrectly()
        {
            // Arrange
            var expectedCursorPosition = new Position(2, 10);
            var snapshot = GetTextSnapshot($@"
@{{
    <text>{LanguageServerConstants.CursorPlaceholderString}</text>
}}");
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = $"{LanguageServerConstants.CursorPlaceholderString}</text>",
                    Range = new Range() { Start = expectedCursorPosition, End = expectedCursorPosition },
                }
            };

            // Act
            var cursorPosition = DefaultLSPEditorService.ExtractCursorPlaceholder(snapshot, edits);

            // Assert
            Assert.Equal(expectedCursorPosition, cursorPosition);
        }

        [Fact]
        public void ExtractsCursorPlaceholder_MultipleEdits_AppliesEditsCorrectly()
        {
            // Arrange
            var expectedCursorPosition = new Position(2, 10);
            var snapshot = GetTextSnapshot($@"
@{{
    <text>{LanguageServerConstants.CursorPlaceholderString}</text>
}}");
            var edits = new[]
            {
                new TextEdit()
                {
                    NewText = $"unrelated Edit",
                    Range = new Range() { Start = new Position(0, 0), End = new Position(0, 1) },
                },
                new TextEdit()
                {
                    NewText = $"{LanguageServerConstants.CursorPlaceholderString}</text>",
                    Range = new Range() { Start = expectedCursorPosition, End = expectedCursorPosition },
                }
            };

            // Act
            var cursorPosition = DefaultLSPEditorService.ExtractCursorPlaceholder(snapshot, edits);

            // Assert
            Assert.Equal(expectedCursorPosition, cursorPosition);
        }

        private ITextSnapshot GetTextSnapshot(string text)
        {
            var snapshot = new StringTextSnapshot(text);
            var buffer = new Mock<ITextBuffer>();
            buffer.Setup(b => b.CreateEdit()).Returns(Mock.Of<ITextEdit>(e => e.Snapshot == snapshot));
            snapshot.TextBuffer = buffer.Object;
            return snapshot;
        }
    }
}
