// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    public class DefaultLSPDocumentTest
    {
        public DefaultLSPDocumentTest()
        {
            Uri = new Uri("C:/path/to/file.razor__virtual.cs");
        }

        private Uri Uri { get; }

        [Fact]
        public void UpdateVirtualDocument_UpdatesProvidedVirtualDocumentWithProvidedArgs_AndRecalcsSnapshot()
        {
            // Arrange
            var snapshot = Mock.Of<ITextSnapshot>(s => s.Version == Mock.Of<ITextVersion>());
            var textBuffer = Mock.Of<ITextBuffer>(buffer => buffer.CurrentSnapshot == snapshot);
            var virtualDocument = new TestVirtualDocument();
            using var document = new DefaultLSPDocument(Uri, textBuffer, new[] { virtualDocument });
            var changes = Array.Empty<ITextChange>();
            var originalSnapshot = document.CurrentSnapshot;

            // Act
            document.UpdateVirtualDocument<TestVirtualDocument>(changes, hostDocumentVersion: 1337);

            // Assert
            Assert.Equal(1337, virtualDocument.HostDocumentSyncVersion);
            Assert.Same(changes, virtualDocument.Changes);
            Assert.NotEqual(originalSnapshot, document.CurrentSnapshot);
        }

        private class TestVirtualDocument : VirtualDocument
        {
            private long? _hostDocumentVersion;

            public IReadOnlyList<ITextChange> Changes { get; private set; }

            public override Uri Uri => throw new NotImplementedException();

            public override ITextBuffer TextBuffer => throw new NotImplementedException();

            public override VirtualDocumentSnapshot CurrentSnapshot => null;

            public override long? HostDocumentSyncVersion => _hostDocumentVersion;

            public override VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, long hostDocumentVersion)
            {
                _hostDocumentVersion = hostDocumentVersion;
                Changes = changes;

                return null;
            }

            public override void Dispose()
            {
            }
        }
    }
}
