// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class UnsynchronizableContentDocumentProcessedListenerTest : LanguageServerTestBase
    {
        public UnsynchronizableContentDocumentProcessedListenerTest()
        {
            var projectSnapshotManager = new Mock<ProjectSnapshotManager>(MockBehavior.Strict);
            projectSnapshotManager.Setup(psm => psm.IsDocumentOpen(It.IsAny<string>()))
                .Returns(true);
            ProjectSnapshotManager = projectSnapshotManager.Object;
        }

        private ProjectSnapshotManager ProjectSnapshotManager { get; }

        [Fact]
        public void DocumentProcessed_DoesNothingForOldDocuments()
        {
            // Arrange
            var generatedDocumentPublisher = new Mock<GeneratedDocumentPublisher>(MockBehavior.Strict);
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, int?>());
            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, generatedDocumentPublisher.Object);
            listener.Initialize(ProjectSnapshotManager);
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml");

            // Act & Assert
            listener.DocumentProcessed(document);
        }

        [Fact]
        public void DocumentProcessed_DoesNothingIfAlreadySynchronized()
        {
            // Arrange
            var generatedDocumentPublisher = new Mock<GeneratedDocumentPublisher>(MockBehavior.Strict);
            var documentVersion = VersionStamp.Default.GetNewerVersion();
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml", documentVersion);
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, int?>()
            {
                [document] = 1337,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());
            var htmlDocument = RazorHtmlDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault());
            var codeDocument = CreateCodeDocument(csharpDocument, htmlDocument);

            // Force the state to already be up-to-date
            document.State.HostDocument.GeneratedDocumentContainer.SetOutput(document, codeDocument, documentVersion.GetNewerVersion(), VersionStamp.Default, VersionStamp.Default);

            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, generatedDocumentPublisher.Object);
            listener.Initialize(ProjectSnapshotManager);

            // Act & Assert
            listener.DocumentProcessed(document);
        }

        [Fact]
        public void DocumentProcessed_DoesNothingForOlderDocuments()
        {
            // Arrange
            var generatedDocumentPublisher = new Mock<GeneratedDocumentPublisher>(MockBehavior.Strict);
            var lastVersion = VersionStamp.Default.GetNewerVersion();
            var lastDocument = TestDocumentSnapshot.Create("C:/path/old.cshtml", lastVersion);
            var oldDocument = TestDocumentSnapshot.Create("C:/path/file.cshtml", VersionStamp.Default);
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, int?>()
            {
                [oldDocument] = 1337,
                [lastDocument] = 1338,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());
            var htmlDocument = RazorHtmlDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault());
            var codeDocument = CreateCodeDocument(csharpDocument, htmlDocument);

            // Force the state to already be up-to-date
            oldDocument.State.HostDocument.GeneratedDocumentContainer.SetOutput(lastDocument, codeDocument, lastVersion, VersionStamp.Default, VersionStamp.Default);

            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, generatedDocumentPublisher.Object);
            listener.Initialize(ProjectSnapshotManager);

            // Act & Assert
            listener.DocumentProcessed(oldDocument);
        }

        [Fact]
        public void DocumentProcessed_DoesNothingIfSourceVersionsAreDifferent()
        {
            // Arrange
            var generatedDocumentPublisher = new Mock<GeneratedDocumentPublisher>(MockBehavior.Strict);
            var lastVersion = VersionStamp.Default.GetNewerVersion();
            var lastDocument = TestDocumentSnapshot.Create("C:/path/old.cshtml", lastVersion);
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml", VersionStamp.Default);
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, int?>()
            {
                [document] = 1338,
                [lastDocument] = 1337,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());
            var htmlDocument = RazorHtmlDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault());
            var codeDocument = CreateCodeDocument(csharpDocument, htmlDocument);

            // Force the state to already be up-to-date
            document.State.HostDocument.GeneratedDocumentContainer.SetOutput(lastDocument, codeDocument, lastVersion, VersionStamp.Default, VersionStamp.Default);

            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, generatedDocumentPublisher.Object);
            listener.Initialize(ProjectSnapshotManager);

            // Act & Assert
            listener.DocumentProcessed(document);
        }

        [Fact]
        public void DocumentProcessed_SynchronizesIfSourceVersionsAreIdenticalButSyncVersionNewer()
        {
            // Arrange
            var lastVersion = VersionStamp.Default.GetNewerVersion();
            var lastDocument = TestDocumentSnapshot.Create("C:/path/old.cshtml", lastVersion);
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml", lastVersion);
            var generatedDocumentPublisher = new Mock<GeneratedDocumentPublisher>(MockBehavior.Strict);
            generatedDocumentPublisher.Setup(publisher => publisher.PublishCSharp(It.IsAny<string>(), It.IsAny<SourceText>(), It.IsAny<int>()))
                .Callback<string, SourceText, int>((filePath, sourceText, hostDocumentVersion) =>
                {
                    Assert.Equal(document.FilePath, filePath);
                    Assert.Equal(document.State.GeneratedDocumentContainer.CSharpSourceTextContainer.CurrentText.ToString(), sourceText.ToString());
                })
                .Verifiable();
            generatedDocumentPublisher.Setup(publisher => publisher.PublishHtml(It.IsAny<string>(), It.IsAny<SourceText>(), It.IsAny<int>()))
                .Callback<string, SourceText, int>((filePath, sourceText, hostDocumentVersion) =>
                {
                    Assert.Equal(document.FilePath, filePath);
                    Assert.Equal(document.State.GeneratedDocumentContainer.HtmlSourceTextContainer.CurrentText.ToString(), sourceText.ToString());
                })
                .Verifiable();
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, int?>()
            {
                [document] = 1338,
                [lastDocument] = 1337,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());
            var htmlDocument = RazorHtmlDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault());
            var codeDocument = CreateCodeDocument(csharpDocument, htmlDocument);

            // Force the state to already be up-to-date
            document.State.HostDocument.GeneratedDocumentContainer.SetOutput(lastDocument, codeDocument, lastVersion, VersionStamp.Default, VersionStamp.Default);

            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, generatedDocumentPublisher.Object);
            listener.Initialize(ProjectSnapshotManager);

            // Act
            listener.DocumentProcessed(document);

            // Assert
            generatedDocumentPublisher.VerifyAll();
        }

        private static RazorCodeDocument CreateCodeDocument(RazorCSharpDocument csharpDocument, RazorHtmlDocument htmlDocument)
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            codeDocument.SetCSharpDocument(csharpDocument);
            codeDocument.Items[typeof(RazorHtmlDocument)] = htmlDocument;
            return codeDocument;
        }

        private class TestDocumentVersionCache : DocumentVersionCache
        {
            private readonly Dictionary<DocumentSnapshot, int?> _versions;

            public TestDocumentVersionCache(Dictionary<DocumentSnapshot, int?> versions)
            {
                if (versions == null)
                {
                    throw new ArgumentNullException(nameof(versions));
                }

                _versions = versions;
            }

            public override bool TryGetDocumentVersion(DocumentSnapshot documentSnapshot, out int? version)
            {
                return _versions.TryGetValue(documentSnapshot, out version);
            }

            public override void TrackDocumentVersion(DocumentSnapshot documentSnapshot, int version) => throw new NotImplementedException();

            public override void Initialize(ProjectSnapshotManagerBase projectManager)
            {
                throw new NotImplementedException();
            }
        }
    }
}
