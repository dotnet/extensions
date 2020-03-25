// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class HtmlVirtualDocumentTest
    {
        public HtmlVirtualDocumentTest()
        {
            Uri = new Uri("C:/path/to/file.razor__virtual.html");
        }

        private Uri Uri { get; }

        [Fact]
        public void Update_AlwaysSetsHostDocumentSyncVersion_AndUpdatesSnapshot()
        {
            // Arrange
            var textBuffer = Mock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>());
            var document = new HtmlVirtualDocument(Uri, textBuffer);
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
            var insert = new TextChange(new TextSpan(123, 0), "inserted text");
            var edit = new Mock<ITextEdit>();
            edit.Setup(e => e.Insert(insert.Span.Start, insert.NewText)).Verifiable();
            edit.Setup(e => e.Apply()).Verifiable();
            var textBuffer = CreateTextBuffer(edit.Object);
            var document = new HtmlVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { insert }, hostDocumentVersion: 1);

            // Assert
            edit.VerifyAll();
        }

        [Fact]
        public void Update_Replace()
        {
            // Arrange
            var replace = new TextChange(new TextSpan(123, 4), "replaced text");
            var edit = new Mock<ITextEdit>();
            edit.Setup(e => e.Replace(replace.Span.Start, replace.Span.Length, replace.NewText)).Verifiable();
            edit.Setup(e => e.Apply()).Verifiable();
            var textBuffer = CreateTextBuffer(edit.Object);
            var document = new HtmlVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { replace }, hostDocumentVersion: 1);

            // Assert
            edit.VerifyAll();
        }

        [Fact]
        public void Update_Delete()
        {
            // Arrange
            var delete = new TextChange(new TextSpan(123, 4), string.Empty);
            var edit = new Mock<ITextEdit>();
            edit.Setup(e => e.Delete(delete.Span.Start, delete.Span.Length)).Verifiable();
            edit.Setup(e => e.Apply()).Verifiable();
            var textBuffer = CreateTextBuffer(edit.Object);
            var document = new HtmlVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { delete }, hostDocumentVersion: 1);

            // Assert
            edit.VerifyAll();
        }

        [Fact]
        public void Update_MultipleEdits()
        {
            // Arrange
            var replace = new TextChange(new TextSpan(123, 4), "replaced text");
            var delete = new TextChange(new TextSpan(123, 4), string.Empty);
            var edit = new Mock<ITextEdit>();
            edit.Setup(e => e.Delete(delete.Span.Start, delete.Span.Length)).Verifiable();
            edit.Setup(e => e.Replace(replace.Span.Start, replace.Span.Length, replace.NewText)).Verifiable();
            var textBuffer = CreateTextBuffer(edit.Object);
            edit.Setup(e => e.Apply())
                .Returns(textBuffer.CurrentSnapshot).Verifiable();
            var document = new HtmlVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { replace, delete }, hostDocumentVersion: 1);

            // Assert
            edit.VerifyAll();
        }

        [Fact]
        public void Update_RecalculatesSnapshot()
        {
            // Arrange
            var replace = new TextChange(new TextSpan(123, 4), "replaced text");
            var edit = new Mock<ITextEdit>();
            edit.Setup(e => e.Replace(replace.Span.Start, replace.Span.Length, replace.NewText));
            var textBuffer = new Mock<ITextBuffer>();
            var textBufferSnapshot = Mock.Of<ITextSnapshot>();
            textBuffer.Setup(buffer => buffer.CreateEdit())
                .Returns(edit.Object);
            textBuffer.Setup(buffer => buffer.CurrentSnapshot)
                .Returns(() => textBufferSnapshot);
            var editedSnapshot = Mock.Of<ITextSnapshot>();
            edit.Setup(e => e.Apply())
                .Callback(() =>
                {
                    textBufferSnapshot = editedSnapshot;
                });
            var document = new HtmlVirtualDocument(Uri, textBuffer.Object);

            // Act
            document.Update(new[] { replace }, hostDocumentVersion: 1);

            // Assert
            Assert.Same(editedSnapshot, document.CurrentSnapshot.Snapshot);
        }

        public static ITextBuffer CreateTextBuffer(ITextEdit edit)
        {
            var textBuffer = Mock.Of<ITextBuffer>(buffer => buffer.CreateEdit() == edit && buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>());
            return textBuffer;
        }
    }
}
