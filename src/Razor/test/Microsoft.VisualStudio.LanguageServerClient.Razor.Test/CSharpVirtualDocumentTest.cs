// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class CSharpVirtualDocumentTest
    {
        public CSharpVirtualDocumentTest()
        {
            Uri = new Uri("C:/path/to/file.razor__virtual.cs");
        }

        private Uri Uri { get; }

        [Fact]
        public void Update_AlwaysSetsHostDocumentSyncVersion_AndUpdatesSnapshot()
        {
            // Arrange
            var textBuffer = new TestTextBuffer(StringTextSnapshot.Empty);
            var document = new CSharpVirtualDocument(Uri, textBuffer);
            var originalSnapshot = document.CurrentSnapshot;

            // Act
            document.Update(Array.Empty<TextChange>(), hostDocumentVersion: 1337);

            // Assert
            Assert.NotSame(originalSnapshot, document.CurrentSnapshot);
            Assert.Equal(1337, document.HostDocumentSyncVersion);
        }

        [Fact]
        public void Update_Insert()
        {
            // Arrange
            var insert = new TextChange(new TextSpan(0, 0), "inserted text");
            var textBuffer = new TestTextBuffer(StringTextSnapshot.Empty);
            var document = new CSharpVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { insert }, hostDocumentVersion: 1);

            // Assert
            var text = textBuffer.CurrentSnapshot.GetText();
            Assert.Equal(insert.NewText, text);
        }

        [Fact]
        public void Update_Replace()
        {
            // Arrange
            var textBuffer = new TestTextBuffer(new StringTextSnapshot("original"));
            var replace = new TextChange(new TextSpan(0, textBuffer.CurrentSnapshot.Length), "replaced text");
            var document = new CSharpVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { replace }, hostDocumentVersion: 1);

            // Assert
            var text = textBuffer.CurrentSnapshot.GetText();
            Assert.Equal(replace.NewText, text);
        }

        [Fact]
        public void Update_Delete()
        {
            // Arrange
            var textBuffer = new TestTextBuffer(new StringTextSnapshot("Hello World"));
            var delete = new TextChange(new TextSpan(6, 5), string.Empty);
            var document = new CSharpVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { delete }, hostDocumentVersion: 1);

            // Assert
            var text = textBuffer.CurrentSnapshot.GetText();
            Assert.Equal("Hello ", text);
        }

        [Fact]
        public void Update_MultipleEdits()
        {
            // Arrange
            var textBuffer = new TestTextBuffer(new StringTextSnapshot("Hello World"));
            var replace = new TextChange(new TextSpan(6, 5), "Replaced");
            var delete = new TextChange(new TextSpan(0, 6), string.Empty);
            var document = new CSharpVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { replace, delete }, hostDocumentVersion: 1);

            // Assert
            var text = textBuffer.CurrentSnapshot.GetText();
            Assert.Equal("Replaced", text);
        }
    }
}
