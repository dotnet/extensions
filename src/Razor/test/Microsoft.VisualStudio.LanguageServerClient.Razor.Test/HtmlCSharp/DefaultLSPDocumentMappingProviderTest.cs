// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class DefaultLSPDocumentMappingProviderTest
    {
        public Uri RazorFile => new Uri("file:///some/folder/to/file.razor");

        public Uri RazorVirtualCSharpFile => new Uri("file:///some/folder/to/file.razor.g.cs");

        public Uri AnotherRazorFile => new Uri("file:///some/folder/to/anotherfile.razor");

        public Uri AnotherRazorVirtualCSharpFile => new Uri("file:///some/folder/to/anotherfile.razor.g.cs");

        public Uri CSharpFile => new Uri("file:///some/folder/to/csharpfile.cs");

        [Fact]
        public async Task RazorMapToDocumentRangeAsync_InvokesLanguageServer()
        {
            // Arrange
            var uri = new Uri("file:///some/folder/to/file.razor");

            var response = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] {
                    new Range()
                    {
                        Start = new Position(1, 1),
                        End = new Position(3, 3),
                    }
                },
                HostDocumentVersion = 1
            };
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.CustomRequestServerAsync<RazorMapToDocumentRangesParams, RazorMapToDocumentRangesResponse>(LanguageServerConstants.RazorMapToDocumentRangesEndpoint, LanguageServerKind.Razor, It.IsAny<RazorMapToDocumentRangesParams>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(response));

            var documentManager = new TestDocumentManager();
            var mappingProvider = new DefaultLSPDocumentMappingProvider(requestInvoker.Object, documentManager);
            var projectedRange = new Range()
            {
                Start = new Position(10, 10),
                End = new Position(15, 15)
            };

            // Act
            var result = await mappingProvider.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, uri, new[] { projectedRange }, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.HostDocumentVersion);
            var actualRange = result.Ranges[0];
            Assert.Equal(new Position(1, 1), actualRange.Start);
            Assert.Equal(new Position(3, 3), actualRange.End);
        }

        [Fact]
        public async Task RemapWorkspaceEditAsync_RemapsEditsAsExpected()
        {
            // Arrange
            var expectedRange = new TestRange(1, 1, 1, 5);
            var expectedVersion = 1;
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(RazorFile, Mock.Of<LSPDocumentSnapshot>(d => d.Version == expectedVersion && d.Uri == RazorFile));

            var requestInvoker = GetRequestInvoker(new[]
            {
                ((RazorLanguageKind.CSharp, RazorFile, new[] { new TestRange(10, 10, 10, 15) }), (new[] { expectedRange }, expectedVersion))
            });
            var mappingProvider = new DefaultLSPDocumentMappingProvider(requestInvoker, documentManager);

            var workspaceEdit = new TestWorkspaceEdit(versionedEdits: true);
            workspaceEdit.AddEdits(RazorVirtualCSharpFile, 10, new TestTextEdit("newText", new TestRange(10, 10, 10, 15)));

            // Act
            var result = await mappingProvider.RemapWorkspaceEditAsync(workspaceEdit, CancellationToken.None).ConfigureAwait(false);

            // Assert
            var documentEdit = Assert.Single(result.DocumentChanges);
            Assert.Equal(RazorFile, documentEdit.TextDocument.Uri);
            Assert.Equal(expectedVersion, documentEdit.TextDocument.Version);

            var actualEdit = Assert.Single(documentEdit.Edits);
            Assert.Equal("newText", actualEdit.NewText);
            Assert.Equal(expectedRange, actualEdit.Range);
        }

        [Fact]
        public async Task RemapWorkspaceEditAsync_DocumentChangesNull_RemapsEditsAsExpected()
        {
            // Arrange
            var expectedRange = new TestRange(1, 1, 1, 5);
            var expectedVersion = 1;
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(RazorFile, Mock.Of<LSPDocumentSnapshot>(d => d.Version == expectedVersion && d.Uri == RazorFile));

            var requestInvoker = GetRequestInvoker(new[]
            {
                ((RazorLanguageKind.CSharp, RazorFile, new[] { new TestRange(10, 10, 10, 15) }), (new[] { expectedRange }, expectedVersion))
            });
            var mappingProvider = new DefaultLSPDocumentMappingProvider(requestInvoker, documentManager);

            var workspaceEdit = new TestWorkspaceEdit(versionedEdits: false);
            workspaceEdit.AddEdits(RazorVirtualCSharpFile, 10, new TestTextEdit("newText", new TestRange(10, 10, 10, 15)));

            // Act
            var result = await mappingProvider.RemapWorkspaceEditAsync(workspaceEdit, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result.DocumentChanges);
            var change = Assert.Single(result.Changes);
            Assert.Equal(RazorFile.AbsoluteUri, change.Key);

            var actualEdit = Assert.Single(change.Value);
            Assert.Equal("newText", actualEdit.NewText);
            Assert.Equal(expectedRange, actualEdit.Range);
        }

        [Fact]
        public async Task RemapWorkspaceEditAsync_DoesNotRemapsNonRazorFiles()
        {
            // Arrange
            var expectedRange = new TestRange(10, 10, 10, 15);
            var expectedVersion = 10;
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(CSharpFile, Mock.Of<LSPDocumentSnapshot>());

            var requestInvoker = GetRequestInvoker(mappingPairs: null); // will throw if RequestInvoker is called.
            var mappingProvider = new DefaultLSPDocumentMappingProvider(requestInvoker, documentManager);

            var workspaceEdit = new TestWorkspaceEdit(versionedEdits: true);
            workspaceEdit.AddEdits(CSharpFile, expectedVersion, new TestTextEdit("newText", expectedRange));

            // Act
            var result = await mappingProvider.RemapWorkspaceEditAsync(workspaceEdit, CancellationToken.None).ConfigureAwait(false);

            // Assert
            var documentEdit = Assert.Single(result.DocumentChanges);
            Assert.Equal(CSharpFile, documentEdit.TextDocument.Uri);
            Assert.Equal(expectedVersion, documentEdit.TextDocument.Version);

            var actualEdit = Assert.Single(documentEdit.Edits);
            Assert.Equal("newText", actualEdit.NewText);
            Assert.Equal(expectedRange, actualEdit.Range);
        }

        [Fact]
        public async Task RemapWorkspaceEditAsync_RemapsMultipleRazorFiles()
        {
            // Arrange
            var expectedRange1 = new TestRange(1, 1, 1, 5);
            var expectedRange2 = new TestRange(5, 5, 5, 10);
            var expectedVersion1 = 1;
            var expectedVersion2 = 5;
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(RazorFile, Mock.Of<LSPDocumentSnapshot>(d => d.Version == expectedVersion1 && d.Uri == RazorFile));
            documentManager.AddDocument(AnotherRazorFile, Mock.Of<LSPDocumentSnapshot>(d => d.Version == expectedVersion2 && d.Uri == AnotherRazorFile));

            var requestInvoker = GetRequestInvoker(new[]
            {
                ((RazorLanguageKind.CSharp, RazorFile, new[] { new TestRange(10, 10, 10, 15) }), (new[] { expectedRange1 }, expectedVersion1)),
                ((RazorLanguageKind.CSharp, AnotherRazorFile, new[] { new TestRange(20, 20, 20, 25) }), (new[] { expectedRange2 }, expectedVersion2))
            });
            var mappingProvider = new DefaultLSPDocumentMappingProvider(requestInvoker, documentManager);

            var workspaceEdit = new TestWorkspaceEdit(versionedEdits: true);
            workspaceEdit.AddEdits(RazorVirtualCSharpFile, 10, new TestTextEdit("newText", new TestRange(10, 10, 10, 15)));
            workspaceEdit.AddEdits(AnotherRazorVirtualCSharpFile, 20, new TestTextEdit("newText", new TestRange(20, 20, 20, 25)));

            // Act
            var result = await mappingProvider.RemapWorkspaceEditAsync(workspaceEdit, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Collection(result.DocumentChanges,
                d =>
                {
                    Assert.Equal(RazorFile, d.TextDocument.Uri);
                    Assert.Equal(expectedVersion1, d.TextDocument.Version);

                    var actualEdit = Assert.Single(d.Edits);
                    Assert.Equal("newText", actualEdit.NewText);
                    Assert.Equal(expectedRange1, actualEdit.Range);
                },
                d =>
                {
                    Assert.Equal(AnotherRazorFile, d.TextDocument.Uri);
                    Assert.Equal(expectedVersion2, d.TextDocument.Version);

                    var actualEdit = Assert.Single(d.Edits);
                    Assert.Equal("newText", actualEdit.NewText);
                    Assert.Equal(expectedRange2, actualEdit.Range);
                });
        }

        private LSPRequestInvoker GetRequestInvoker(((RazorLanguageKind, Uri, TestRange[]), (TestRange[], int))[] mappingPairs)
        {
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            if (mappingPairs == null)
            {
                return requestInvoker.Object;
            }

            // mappingPairs will contain the request/response pair for each of MapToDocumentRange LSP request we want to mock.
            foreach (var ((kind, uri, projectedRanges), (mappedRanges, version)) in mappingPairs)
            {
                var requestParams = new RazorMapToDocumentRangesParams()
                {
                    Kind = kind,
                    RazorDocumentUri = uri,
                    ProjectedRanges = projectedRanges
                };
                var response = new RazorMapToDocumentRangesResponse()
                {
                    Ranges = mappedRanges,
                    HostDocumentVersion = version
                };

                requestInvoker
                    .Setup(r => r.CustomRequestServerAsync<RazorMapToDocumentRangesParams, RazorMapToDocumentRangesResponse>(LanguageServerConstants.RazorMapToDocumentRangesEndpoint, LanguageServerKind.Razor, requestParams, It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(response));
            }

            return requestInvoker.Object;
        }

        private class TestWorkspaceEdit : WorkspaceEdit
        {
            public TestWorkspaceEdit(bool versionedEdits = false)
            {
                if (versionedEdits)
                {
                    DocumentChanges = Array.Empty<TextDocumentEdit>();
                }

                Changes = new Dictionary<string, TextEdit[]>();
            }

            public void AddEdits(Uri uri, int version, params TextEdit[] edits)
            {
                Changes[uri.AbsoluteUri] = edits;

                DocumentChanges = DocumentChanges?.Append(new TextDocumentEdit()
                {
                    Edits = edits,
                    TextDocument = new VersionedTextDocumentIdentifier()
                    {
                        Uri = uri,
                        Version = version
                    }
                }).ToArray();
            }
        }

        private class TestTextEdit : TextEdit
        {
            public TestTextEdit(string newText, Range range)
            {
                NewText = newText;
                Range = range;
            }
        }

        private class TestRange : Range
        {
            public TestRange(int startLine, int startCharacter, int endLine, int endCharacter)
            {
                Start = new Position(startLine, startCharacter);
                End = new Position(endLine, endCharacter);
            }
        }
    }
}
