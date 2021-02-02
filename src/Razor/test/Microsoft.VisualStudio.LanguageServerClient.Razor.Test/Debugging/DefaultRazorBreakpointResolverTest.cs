// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.Test;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    public class DefaultRazorBreakpointResolverTest
    {
        public DefaultRazorBreakpointResolverTest()
        {
            DocumentUri = new Uri("file://C:/path/to/file.razor", UriKind.Absolute);

            ValidBreakpointCSharp = "private int foo = 123;";
            InvalidBreakpointCSharp = "private int bar;";
            var mappedCSharpText =
$@"
    {ValidBreakpointCSharp}
    {InvalidBreakpointCSharp}
";
            var csharpTextSnapshot = new StringTextSnapshot(
$@"public class SomeRazorFile
{{{mappedCSharpText}}}");
            CSharpTextBuffer = new TestTextBuffer(csharpTextSnapshot);

            var textBufferSnapshot = new StringTextSnapshot($"@code {{{mappedCSharpText}}}");
            HostTextbuffer = new TestTextBuffer(textBufferSnapshot);
        }

        private string ValidBreakpointCSharp { get; }

        private string InvalidBreakpointCSharp { get; }

        private ITextBuffer CSharpTextBuffer { get; }

        private Uri DocumentUri { get; }

        private ITextBuffer HostTextbuffer { get; }

        [Fact]
        public async Task TryResolveBreakpointRangeAsync_UnaddressableTextBuffer_ReturnsNull()
        {
            // Arrange
            var differentTextBuffer = Mock.Of<ITextBuffer>(MockBehavior.Strict);
            var resolver = CreateResolverWith();

            // Act
            var breakpointRange = await resolver.TryResolveBreakpointRangeAsync(differentTextBuffer, lineIndex: 0, characterIndex: 1, CancellationToken.None);

            // Assert
            Assert.Null(breakpointRange);
        }

        [Fact]
        public async Task TryResolveBreakpointRangeAsync_UnknownRazorDocument_ReturnsNull()
        {
            // Arrange
            var documentManager = new Mock<LSPDocumentManager>(MockBehavior.Strict).Object;
            Mock.Get(documentManager).Setup(m => m.TryGetDocument(DocumentUri, out It.Ref<LSPDocumentSnapshot>.IsAny)).Returns(false);
            var resolver = CreateResolverWith(documentManager: documentManager);

            // Act
            var result = await resolver.TryResolveBreakpointRangeAsync(HostTextbuffer, lineIndex: 0, characterIndex: 1, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task TryResolveBreakpointRangeAsync_UnprojectedLocation_ReturnsNull()
        {
            // Arrange
            var resolver = CreateResolverWith();

            // Act
            var breakpointRange = await resolver.TryResolveBreakpointRangeAsync(HostTextbuffer, lineIndex: 0, characterIndex: 1, CancellationToken.None);

            // Assert
            Assert.Null(breakpointRange);
        }

        [Fact]
        public async Task TryResolveBreakpointRangeAsync_RazorProjectedLocation_ReturnsNull()
        {
            // Arrange
            var position = new Position(line: 0, character: 2);
            var projectionProvider = new TestLSPProjectionProvider(
                DocumentUri,
                new Dictionary<Position, ProjectionResult>()
                {
                    [position] = new ProjectionResult()
                    {
                        LanguageKind = RazorLanguageKind.Razor,
                        HostDocumentVersion = 0,
                        Position = position,
                    }
                });
            var resolver = CreateResolverWith(projectionProvider: projectionProvider);

            // Act
            var breakpointRange = await resolver.TryResolveBreakpointRangeAsync(HostTextbuffer, position.Line, position.Character, CancellationToken.None);

            // Assert
            Assert.Null(breakpointRange);
        }

        [Fact]
        public async Task TryResolveBreakpointRangeAsync_NotValidBreakpointLocation_ReturnsNull()
        {
            // Arrange
            var hostDocumentPosition = GetPosition(InvalidBreakpointCSharp, HostTextbuffer);
            var csharpDocumentPosition = GetPosition(InvalidBreakpointCSharp, CSharpTextBuffer);
            var csharpDocumentIndex = CSharpTextBuffer.CurrentSnapshot.GetText().IndexOf(InvalidBreakpointCSharp, StringComparison.Ordinal);
            var projectionProvider = new TestLSPProjectionProvider(
                DocumentUri,
                new Dictionary<Position, ProjectionResult>()
                {
                    [hostDocumentPosition] = new ProjectionResult()
                    {
                        LanguageKind = RazorLanguageKind.CSharp,
                        HostDocumentVersion = 0,
                        Position = csharpDocumentPosition,
                        PositionIndex = csharpDocumentIndex,
                    }
                });
            var resolver = CreateResolverWith(projectionProvider: projectionProvider);

            // Act
            var breakpointRange = await resolver.TryResolveBreakpointRangeAsync(HostTextbuffer, hostDocumentPosition.Line, hostDocumentPosition.Character, CancellationToken.None);

            // Assert
            Assert.Null(breakpointRange);
        }

        [Fact]
        public async Task TryResolveBreakpointRangeAsync_UnmappableCSharpBreakpointLocation_ReturnsNull()
        {
            // Arrange
            var hostDocumentPosition = GetPosition(ValidBreakpointCSharp, HostTextbuffer);
            var csharpDocumentPosition = GetPosition(ValidBreakpointCSharp, CSharpTextBuffer);
            var csharpDocumentIndex = CSharpTextBuffer.CurrentSnapshot.GetText().IndexOf(ValidBreakpointCSharp, StringComparison.Ordinal);
            var projectionProvider = new TestLSPProjectionProvider(
                DocumentUri,
                new Dictionary<Position, ProjectionResult>()
                {
                    [hostDocumentPosition] = new ProjectionResult()
                    {
                        LanguageKind = RazorLanguageKind.CSharp,
                        HostDocumentVersion = 0,
                        Position = csharpDocumentPosition,
                        PositionIndex = csharpDocumentIndex,
                    }
                });
            var resolver = CreateResolverWith(projectionProvider: projectionProvider);

            // Act
            var breakpointRange = await resolver.TryResolveBreakpointRangeAsync(HostTextbuffer, hostDocumentPosition.Line, hostDocumentPosition.Character, CancellationToken.None);

            // Assert
            Assert.Null(breakpointRange);
        }

        [Fact]
        public async Task TryResolveBreakpointRangeAsync_MappableCSharpBreakpointLocation_ReturnsHostBreakpointLocation()
        {
            // Arrange
            var hostDocumentPosition = GetPosition(ValidBreakpointCSharp, HostTextbuffer);
            var csharpDocumentPosition = GetPosition(ValidBreakpointCSharp, CSharpTextBuffer);
            var csharpDocumentIndex = CSharpTextBuffer.CurrentSnapshot.GetText().IndexOf(ValidBreakpointCSharp, StringComparison.Ordinal);
            var projectionProvider = new TestLSPProjectionProvider(
                DocumentUri,
                new Dictionary<Position, ProjectionResult>()
                {
                    [hostDocumentPosition] = new ProjectionResult()
                    {
                        LanguageKind = RazorLanguageKind.CSharp,
                        HostDocumentVersion = 0,
                        Position = csharpDocumentPosition,
                        PositionIndex = csharpDocumentIndex,
                    }
                });
            var expectedCSharpBreakpointRange = new Range()
            {
                Start = csharpDocumentPosition,
                End = new Position(csharpDocumentPosition.Line, csharpDocumentPosition.Character + ValidBreakpointCSharp.Length),
            };
            var hostBreakpointRange = new Range()
            {
                Start = hostDocumentPosition,
                End = new Position(hostDocumentPosition.Line, hostDocumentPosition.Character + ValidBreakpointCSharp.Length),
            };
            var mappingProvider = new TestLSPDocumentMappingProvider(
                new Dictionary<Range, RazorMapToDocumentRangesResponse>()
                {
                    [expectedCSharpBreakpointRange] = new RazorMapToDocumentRangesResponse()
                    {
                        HostDocumentVersion = 0,
                        Ranges = new[]
                        {
                            hostBreakpointRange,
                        },
                    }
                });
            var resolver = CreateResolverWith(projectionProvider: projectionProvider, documentMappingProvider: mappingProvider);

            // Act
            var breakpointRange = await resolver.TryResolveBreakpointRangeAsync(HostTextbuffer, hostDocumentPosition.Line, hostDocumentPosition.Character, CancellationToken.None);

            // Assert
            Assert.Equal(hostBreakpointRange, breakpointRange);
        }

        private RazorBreakpointResolver CreateResolverWith(
            FileUriProvider uriProvider = null,
            LSPDocumentManager documentManager = null,
            LSPProjectionProvider projectionProvider = null,
            LSPDocumentMappingProvider documentMappingProvider = null)
        {
            var documentUri = DocumentUri;
            uriProvider ??= Mock.Of<FileUriProvider>(provider => provider.TryGet(HostTextbuffer, out documentUri) == true && provider.TryGet(It.IsNotIn(HostTextbuffer), out It.Ref<Uri>.IsAny) == false, MockBehavior.Strict);
            var csharpDocumentUri = new Uri(DocumentUri.OriginalString + ".g.cs", UriKind.Absolute);
            var csharpVirtualDocumentSnapshot = new CSharpVirtualDocumentSnapshot(csharpDocumentUri, CSharpTextBuffer.CurrentSnapshot, hostDocumentSyncVersion: 0);
            LSPDocumentSnapshot documentSnapshot = new TestLSPDocumentSnapshot(DocumentUri, 0, csharpVirtualDocumentSnapshot);
            documentManager ??= Mock.Of<LSPDocumentManager>(manager => manager.TryGetDocument(DocumentUri, out documentSnapshot) == true, MockBehavior.Strict);
            if (projectionProvider is null)
            {
                projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict).Object;
                Mock.Get(projectionProvider).Setup(projectionProvider => projectionProvider.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), CancellationToken.None))
                    .Returns(Task.FromResult<ProjectionResult>(null));
            }

            if (documentMappingProvider is null)
            {
                documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict).Object;
                Mock.Get(documentMappingProvider).Setup(p => p.MapToDocumentRangesAsync(It.IsAny<RazorLanguageKind>(), It.IsAny<Uri>(), It.IsAny<Range[]>(), CancellationToken.None))
                    .Returns(Task.FromResult<RazorMapToDocumentRangesResponse>(null));
            }

            var csharpBreakpointResolver = new DefaultCSharpBreakpointResolver();
            var razorBreakpointResolver = new DefaultRazorBreakpointResolver(
                uriProvider,
                documentManager,
                projectionProvider,
                documentMappingProvider,
                csharpBreakpointResolver);

            return razorBreakpointResolver;
        }

        private Position GetPosition(string content, ITextBuffer textBuffer)
        {
            var index = textBuffer.CurrentSnapshot.GetText().IndexOf(content, StringComparison.Ordinal);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(content));
            }

            textBuffer.CurrentSnapshot.GetLineAndCharacter(index, out var lineIndex, out var characterIndex);
            return new Position(lineIndex, characterIndex);
        }

        private class TestLSPDocumentMappingProvider : LSPDocumentMappingProvider
        {
            private readonly IReadOnlyDictionary<Range, RazorMapToDocumentRangesResponse> _mappings;

            public TestLSPDocumentMappingProvider(IReadOnlyDictionary<Range, RazorMapToDocumentRangesResponse> mappings)
            {
                if (mappings is null)
                {
                    throw new ArgumentNullException(nameof(mappings));
                }

                _mappings = mappings;
            }

            public override Task<RazorMapToDocumentRangesResponse> MapToDocumentRangesAsync(RazorLanguageKind languageKind, Uri razorDocumentUri, Range[] projectedRanges, CancellationToken cancellationToken)
                => MapToDocumentRangesAsync(languageKind, razorDocumentUri, projectedRanges, LanguageServerMappingBehavior.Strict, cancellationToken);

            public override Task<RazorMapToDocumentRangesResponse> MapToDocumentRangesAsync(
                RazorLanguageKind languageKind,
                Uri razorDocumentUri,
                Range[] projectedRanges,
                LanguageServerMappingBehavior mappingBehavior,
                CancellationToken cancellationToken)
            {
                _mappings.TryGetValue(projectedRanges[0], out var response);
                return Task.FromResult(response);
            }

            public override Task<TextEdit[]> RemapFormattedTextEditsAsync(Uri uri, TextEdit[] edits, FormattingOptions options, bool containsSnippet, CancellationToken cancellationToken) => throw new NotImplementedException();

            public override Task<Location[]> RemapLocationsAsync(Location[] locations, CancellationToken cancellationToken) => throw new NotImplementedException();

            public override Task<TextEdit[]> RemapTextEditsAsync(Uri uri, TextEdit[] edits, CancellationToken cancellationToken) => throw new NotImplementedException();

            public override Task<WorkspaceEdit> RemapWorkspaceEditAsync(WorkspaceEdit workspaceEdit, CancellationToken cancellationToken) => throw new NotImplementedException();
        }

        private class TestLSPProjectionProvider : LSPProjectionProvider
        {
            private readonly Uri _documentUri;
            private readonly IReadOnlyDictionary<Position, ProjectionResult> _mappings;

            public TestLSPProjectionProvider(Uri documentUri, IReadOnlyDictionary<Position, ProjectionResult> mappings)
            {
                if (documentUri is null)
                {
                    throw new ArgumentNullException(nameof(documentUri));
                }

                if (mappings is null)
                {
                    throw new ArgumentNullException(nameof(mappings));
                }

                _documentUri = documentUri;
                _mappings = mappings;
            }

            public override Task<ProjectionResult> GetProjectionAsync(LSPDocumentSnapshot documentSnapshot, Position position, CancellationToken cancellationToken)
            {
                if (documentSnapshot.Uri != _documentUri)
                {
                    return Task.FromResult((ProjectionResult)null);
                }

                _mappings.TryGetValue(position, out var projectionResult);

                return Task.FromResult(projectionResult);
            }
        }
    }
}
