// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Moq;
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
            var textBuffer = Mock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>());
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
            var insert = new TextChange(new TextSpan(123, 0), "inserted text");
            var edit = new Mock<ITextEdit>();
            edit.Setup(e => e.Insert(insert.Span.Start, insert.NewText)).Verifiable();
            edit.Setup(e => e.Apply()).Verifiable();
            var textBuffer = CreateTextBuffer(edit.Object);
            var document = new CSharpVirtualDocument(Uri, textBuffer);

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
            var document = new CSharpVirtualDocument(Uri, textBuffer);

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
            var document = new CSharpVirtualDocument(Uri, textBuffer);

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
            var document = new CSharpVirtualDocument(Uri, textBuffer);

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
            var document = new CSharpVirtualDocument(Uri, textBuffer.Object);

            // Act
            document.Update(new[] { replace }, hostDocumentVersion: 1);

            // Assert
            Assert.Same(editedSnapshot, document.CurrentSnapshot.Snapshot);
        }

        [Fact]
        public void Update_Provisional_AppliesAndRevertsProvisionalChanges()
        {
            // Arrange
            var insert = new TextChange(new TextSpan(123, 0), ".");
            var edit = new Mock<ITextEdit>();
            edit.Setup(e => e.Insert(insert.Span.Start, insert.NewText)).Verifiable();
            edit.Setup(e => e.Apply()).Verifiable();

            var revertEdit = new Mock<ITextEdit>();
            revertEdit.Setup(e => e.Replace(new Span(123, 1), string.Empty)).Verifiable();
            revertEdit.Setup(e => e.Apply()).Verifiable();

            var textBuffer = CreateTextBuffer(edit.Object, revertEdit.Object, new[] { insert });
            var document = new CSharpVirtualDocument(Uri, textBuffer);

            // Make a provisional edit followed by another edit.

            // Act 1
            document.Update(new[] { insert }, hostDocumentVersion: 1, provisional: true);

            // Assert 1
            edit.VerifyAll();

            // Act 2
            document.Update(new[] { new TextChange(new TextSpan(125, 0), "Some other edit") }, hostDocumentVersion: 2, provisional: false);

            // Assert 2
            revertEdit.VerifyAll();
        }

        public static ITextBuffer CreateTextBuffer(ITextEdit edit, ITextEdit revertEdit = null, TextChange[] provisionalChanges = null)
        {
            var changes = new TestTextChangeCollection();
            if (provisionalChanges != null)
            {
                foreach (var provisionalChange in provisionalChanges)
                {
                    var change = new Mock<ITextChange>();
                    change.SetupGet(c => c.NewSpan).Returns(new Span(provisionalChange.Span.Start, provisionalChange.NewText.Length));
                    change.SetupGet(c => c.OldText).Returns(string.Empty);
                    changes.Add(change.Object);
                }
            }

            var textBuffer = Mock.Of<ITextBuffer>(
                buffer => buffer.CreateEdit() == edit &&
                buffer.CreateEdit(EditOptions.None, It.IsAny<int?>(), It.IsAny<IInviolableEditTag>()) == revertEdit &&
                buffer.CurrentSnapshot == Mock.Of<ITextSnapshot>(s => s.Version.Changes == changes));
            return textBuffer;
        }

        protected class TestTextChangeCollection : List<ITextChange>, INormalizedTextChangeCollection
        {
            public bool IncludesLineChanges => true;
        }
    }
}
