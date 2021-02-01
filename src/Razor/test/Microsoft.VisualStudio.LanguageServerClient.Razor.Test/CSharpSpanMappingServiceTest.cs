// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using RazorMapToDocumentRangesResponse = Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp.RazorMapToDocumentRangesResponse;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class CSharpSpanMappingServiceTest
    {
        private readonly Uri MockDocumentUri = new Uri("C://project/path/document.razor");

        private static readonly string MockGeneratedContent = $"Hello {Environment.NewLine} This is the source text in the generated C# file. {Environment.NewLine} This is some more sample text for demo purposes.";
        private static readonly string MockRazorContent = $"Hello {Environment.NewLine} This is the {Environment.NewLine} source text {Environment.NewLine} in the generated C# file. {Environment.NewLine} This is some more sample text for demo purposes.";

        private readonly SourceText SourceTextGenerated = SourceText.From(MockGeneratedContent);
        private readonly SourceText SourceTextRazor = SourceText.From(MockRazorContent);

        [Fact]
        public async Task MapSpans_WithinRange_ReturnsMapping()
        {
            // Arrange
            var called = false;

            var textSpan = new TextSpan(1, 10);
            var spans = new TextSpan[] { textSpan };

            var documentSnapshot = new Mock<LSPDocumentSnapshot>(MockBehavior.Strict);
            documentSnapshot.SetupGet(doc => doc.Uri).Returns(MockDocumentUri);

            var textSnapshot = new StringTextSnapshot(MockGeneratedContent, 1);

            var textSpanAsRange = textSpan.AsLSPRange(SourceTextGenerated);
            var mappedRange = new Range()
            {
                Start = new Position(2, 1),
                End = new Position(2, 11)
            };

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            var mappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new Range[] { mappedRange }
            };
            documentMappingProvider.Setup(dmp => dmp.MapToDocumentRangesAsync(It.IsAny<RazorLanguageKind>(), It.IsAny<Uri>(), It.IsAny<Range[]>(), It.IsAny<CancellationToken>()))
                .Callback<RazorLanguageKind, Uri, Range[], CancellationToken>((languageKind, Uri, ranges, ct) =>
                {
                    Assert.Equal(RazorLanguageKind.CSharp, languageKind);
                    Assert.Equal(MockDocumentUri, Uri);
                    Assert.Single(ranges, textSpanAsRange);
                    called = true;
                })
                .Returns(Task.FromResult(mappingResult));

            var service = new CSharpSpanMappingService(documentMappingProvider.Object, documentSnapshot.Object, textSnapshot);

            var expectedSpan = mappedRange.AsTextSpan(SourceTextRazor);
            var expectedLinePosition = SourceTextRazor.Lines.GetLinePositionSpan(expectedSpan);
            var expectedFilePath = MockDocumentUri.LocalPath;
            var expectedResult = (expectedFilePath, expectedLinePosition, expectedSpan);

            // Act
            var result = await service.MapSpansAsyncTest(spans, SourceTextGenerated, SourceTextRazor).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Single(result, expectedResult);
        }

        [Fact]
        public async Task MapSpans_OutsideRange_ReturnsEmpty()
        {
            // Arrange
            var called = false;

            var textSpan = new TextSpan(10, 10);
            var spans = new TextSpan[] { textSpan };

            var documentSnapshot = new Mock<LSPDocumentSnapshot>(MockBehavior.Strict);
            documentSnapshot.SetupGet(doc => doc.Uri).Returns(MockDocumentUri);

            var textSnapshot = new StringTextSnapshot(MockGeneratedContent, 1);

            var textSpanAsRange = textSpan.AsLSPRange(SourceTextGenerated);

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(dmp => dmp.MapToDocumentRangesAsync(It.IsAny<RazorLanguageKind>(), It.IsAny<Uri>(), It.IsAny<Range[]>(), It.IsAny<CancellationToken>()))
                .Callback<RazorLanguageKind, Uri, Range[], CancellationToken>((languageKind, Uri, ranges, ct) =>
                {
                    Assert.Equal(RazorLanguageKind.CSharp, languageKind);
                    Assert.Equal(MockDocumentUri, Uri);
                    Assert.Single(ranges, textSpanAsRange);
                    called = true;
                })
                .Returns(Task.FromResult<RazorMapToDocumentRangesResponse>(null));

            var service = new CSharpSpanMappingService(documentMappingProvider.Object, documentSnapshot.Object, textSnapshot);

            // Act
            var result = await service.MapSpansAsyncTest(spans, SourceTextGenerated, SourceTextRazor).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Empty(result);
        }
    }
}
