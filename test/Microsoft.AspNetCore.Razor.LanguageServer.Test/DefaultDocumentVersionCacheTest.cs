// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultDocumentVersionCacheTest : TestBase
    {
        [Fact]
        public void TrackDocumentVersion_AddsFirstEntry()
        {
            // Arrange
            var documentVersionCache = new DefaultDocumentVersionCache(Dispatcher);
            var document = TestDocumentSnapshot.Create("C:/file.cshtml");

            // Act
            documentVersionCache.TrackDocumentVersion(document, 1337);

            // Assert
            var kvp = Assert.Single(documentVersionCache._documentLookup);
            Assert.Equal(document.FilePath, kvp.Key);
            var entry = Assert.Single(kvp.Value);
            Assert.True(entry.Document.TryGetTarget(out var actualDocument));
            Assert.Same(document, actualDocument);
            Assert.Equal(1337, entry.Version);
        }

        [Fact]
        public void TrackDocumentVersion_EvictsOldEntries()
        {
            // Arrange
            var documentVersionCache = new DefaultDocumentVersionCache(Dispatcher);
            var document = TestDocumentSnapshot.Create("C:/file.cshtml");

            for (var i = 0; i < DefaultDocumentVersionCache.MaxDocumentTrackingCount; i++)
            {
                documentVersionCache.TrackDocumentVersion(document, i);
            }

            // Act
            documentVersionCache.TrackDocumentVersion(document, 1337);

            // Assert
            var kvp = Assert.Single(documentVersionCache._documentLookup);
            Assert.Equal(DefaultDocumentVersionCache.MaxDocumentTrackingCount, kvp.Value.Count);
            Assert.Equal(1337, kvp.Value.Last().Version);
        }

        [Fact]
        public void TryGetDocumentVersion_UntrackedDocumentPath_ReturnsFalse()
        {
            // Arrange
            var documentVersionCache = new DefaultDocumentVersionCache(Dispatcher);
            var document = TestDocumentSnapshot.Create("C:/file.cshtml");

            // Act
            var result = documentVersionCache.TryGetDocumentVersion(document, out var version);

            // Assert
            Assert.False(result);
            Assert.Equal(-1, version);
        }

        [Fact]
        public void TryGetDocumentVersion_EvictedDocument_ReturnsFalse()
        {
            // Arrange
            var documentVersionCache = new DefaultDocumentVersionCache(Dispatcher);
            var document = TestDocumentSnapshot.Create("C:/file.cshtml");
            var evictedDocument = TestDocumentSnapshot.Create(document.FilePath);
            documentVersionCache.TrackDocumentVersion(document, 1337);

            // Act
            var result = documentVersionCache.TryGetDocumentVersion(evictedDocument, out var version);

            // Assert
            Assert.False(result);
            Assert.Equal(-1, version);
        }

        [Fact]
        public void TryGetDocumentVersion_KnownDocument_ReturnsTrue()
        {
            // Arrange
            var documentVersionCache = new DefaultDocumentVersionCache(Dispatcher);
            var document = TestDocumentSnapshot.Create("C:/file.cshtml");
            documentVersionCache.TrackDocumentVersion(document, 1337);

            // Act
            var result = documentVersionCache.TryGetDocumentVersion(document, out var version);

            // Assert
            Assert.True(result);
            Assert.Equal(1337, version);
        }
    }
}
