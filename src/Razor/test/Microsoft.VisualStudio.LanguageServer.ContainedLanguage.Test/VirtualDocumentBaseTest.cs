using System;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class VirtualDocumentBaseTest
    {
        public VirtualDocumentBaseTest()
        {
            Uri = new Uri("C:/path/to/file.razor__virtual.test");
        }

        private Uri Uri { get; }

        [Fact]
        public void Update_AlwaysSetsHostDocumentSyncVersion_AndUpdatesSnapshot()
        {
            // Arrange
            var textBuffer = new TestTextBuffer(StringTextSnapshot.Empty);
            using var document = new TestVirtualDocument(Uri, textBuffer);
            var originalSnapshot = document.CurrentSnapshot;

            // Act
            document.Update(Array.Empty<ITextChange>(), hostDocumentVersion: 1337);

            // Assert
            Assert.NotSame(originalSnapshot, document.CurrentSnapshot);
            Assert.Equal(1337, document.HostDocumentSyncVersion);
        }

        [Fact]
        public void Update_Insert()
        {
            // Arrange
            var insert = new VisualStudioTextChange(0, 0, "inserted text");
            var textBuffer = new TestTextBuffer(StringTextSnapshot.Empty);
            using var document = new TestVirtualDocument(Uri, textBuffer);

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
            var replace = new VisualStudioTextChange(0, textBuffer.CurrentSnapshot.Length, "replaced text");
            using var document = new TestVirtualDocument(Uri, textBuffer);

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
            var delete = new VisualStudioTextChange(6, 5, string.Empty);
            using var document = new TestVirtualDocument(Uri, textBuffer);

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
            var replace = new VisualStudioTextChange(6, 5, "Replaced");
            var delete = new VisualStudioTextChange(0, 6, string.Empty);
            using var document = new TestVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(new[] { replace, delete }, hostDocumentVersion: 1);

            // Assert
            var text = textBuffer.CurrentSnapshot.GetText();
            Assert.Equal("Replaced", text);
        }

        [Fact]
        public void Update_NoChanges_InvokesPostChangedEventTwice_NoEffectiveChanges()
        {
            // Arrange
            var textBuffer = new TestTextBuffer(new StringTextSnapshot("Hello World"));
            var called = 0;
            textBuffer.PostChanged += (s, a) =>
            {
                textBuffer.TryGetHostDocumentSyncVersion(out var version);
                Assert.Equal(1, version);

                called += 1;
            };

            using var document = new TestVirtualDocument(Uri, textBuffer);

            // Act
            document.Update(Array.Empty<ITextChange>(), hostDocumentVersion: 1);

            // Assert
            Assert.Equal(2, called);
            var text = textBuffer.CurrentSnapshot.GetText();
            Assert.Equal("Hello World", text);
        }
    }
}
