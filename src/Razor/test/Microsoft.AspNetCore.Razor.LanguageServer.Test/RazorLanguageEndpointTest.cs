// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorLanguageEndpointTest : LanguageServerTestBase
    {
        public RazorLanguageEndpointTest()
        {
            var documentVersionCache = new Mock<DocumentVersionCache>();
            int? version = 1337;
            documentVersionCache.Setup(cache => cache.TryGetDocumentVersion(It.IsAny<DocumentSnapshot>(), out version))
                .Returns(true);

            DocumentVersionCache = documentVersionCache.Object;
            MappingService = new DefaultRazorDocumentMappingService();
        }

        private DocumentVersionCache DocumentVersionCache { get; }

        private RazorDocumentMappingService MappingService { get; }

        // These are more integration tests to validate that all the pieces work together
        [Fact]
        public async Task Handle_MapToDocumentRanges_CSharp()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorMapToDocumentRangesParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRanges = new[] { new Range(new Position(0, 10), new Position(0, 22)) },
                RazorDocumentUri = new Uri(documentPath),
            };
            var expectedRange = new Range(new Position(0, 4), new Position(0, 16));

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Ranges[0]);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRanges_CSharp_Unmapped()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorMapToDocumentRangesParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRanges = new[] { new Range(new Position(0, 0), new Position(0, 3)) },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageEndpoint.UndefinedRange, response.Ranges[0]);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRanges_CSharp_LeadingOverlapsUnmapped()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorMapToDocumentRangesParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRanges = new[] { new Range(new Position(0, 0), new Position(0, 22)) },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageEndpoint.UndefinedRange, response.Ranges[0]);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRanges_CSharp_TrailingOverlapsUnmapped()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorMapToDocumentRangesParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRanges = new[] { new Range(new Position(0, 10), new Position(0, 23)) },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageEndpoint.UndefinedRange, response.Ranges[0]);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRanges_Html()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now</p>");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorMapToDocumentRangesParams()
            {
                Kind = RazorLanguageKind.Html,
                ProjectedRanges = new[] { new Range(new Position(0, 16), new Position(0, 20)) },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(request.ProjectedRanges[0], response.Ranges[0]);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRanges_Razor()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now</p>");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorMapToDocumentRangesParams()
            {
                Kind = RazorLanguageKind.Razor,
                ProjectedRanges = new[] { new Range(new Position(0, 3), new Position(0, 4)) },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(request.ProjectedRanges[0], response.Ranges[0]);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRanges_Unsupported()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p>@DateTime.Now</p>",
                "var __o = DateTime.Now",
                new[] {
                    new SourceMapping(
                        new SourceSpan(4, 12),
                        new SourceSpan(10, 12))
                });
            codeDocument.SetUnsupported();
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorMapToDocumentRangesParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRanges = new[] { new Range(new Position(0, 10), new Position(0, 22)) },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageEndpoint.UndefinedRange, response.Ranges[0]);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ResolvesLanguageRequest_Razor()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("@{}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorLanguageQueryParams()
            {
                Uri = new Uri(documentPath),
                Position = new Position(0, 1),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageKind.Razor, response.Kind);
            Assert.Equal(request.Position, response.Position);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_ResolvesLanguageRequest_Html()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("<s");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorLanguageQueryParams()
            {
                Uri = new Uri(documentPath),
                Position = new Position(0, 2),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageKind.Html, response.Kind);
            Assert.Equal(request.Position, response.Position);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_ResolvesLanguageRequest_CSharp()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "@",
                "/* CSharp */",
                new[] { new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 12)) });
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorLanguageQueryParams()
            {
                Uri = new Uri(documentPath),
                Position = new Position(0, 1),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, response.Kind);
            Assert.Equal(0, response.Position.Line);
            Assert.Equal(1, response.Position.Character);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_Unsupported_ResolvesLanguageRequest_Html()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "@",
                "/* CSharp */",
                new[] { new SourceMapping(new SourceSpan(0, 1), new SourceSpan(0, 12)) });
            codeDocument.SetUnsupported();
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, Mock.Of<RazorFormattingService>(), LoggerFactory);
            var request = new RazorLanguageQueryParams()
            {
                Uri = new Uri(documentPath),
                Position = new Position(0, 1),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageKind.Html, response.Kind);
            Assert.Equal(0, response.Position.Line);
            Assert.Equal(1, response.Position.Character);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        private static DocumentResolver CreateDocumentResolver(string documentPath, RazorCodeDocument codeDocument)
        {
            var sourceTextChars = new char[codeDocument.Source.Length];
            codeDocument.Source.CopyTo(0, sourceTextChars, 0, codeDocument.Source.Length);
            var sourceText = SourceText.From(new string(sourceTextChars));
            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(sourceText));
            var documentResolver = new Mock<DocumentResolver>();
            documentResolver.Setup(resolver => resolver.TryResolveDocument(documentPath, out documentSnapshot))
                .Returns(true);
            return documentResolver.Object;
        }

        private static RazorCodeDocument CreateCodeDocument(string text, IReadOnlyList<TagHelperDescriptor> tagHelpers = null)
        {
            tagHelpers = tagHelpers ?? Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, "mvc", Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
        }

        private static RazorCodeDocument CreateCodeDocumentWithCSharpProjection(string razorSource, string projectedCSharpSource, IEnumerable<SourceMapping> sourceMappings)
        {
            var codeDocument = CreateCodeDocument(razorSource, Array.Empty<TagHelperDescriptor>());
            var csharpDocument = RazorCSharpDocument.Create(
                    projectedCSharpSource,
                    RazorCodeGenerationOptions.CreateDefault(),
                    Enumerable.Empty<RazorDiagnostic>(),
                    sourceMappings,
                    Enumerable.Empty<LinePragma>());
            codeDocument.SetCSharpDocument(csharpDocument);
            return codeDocument;
        }
    }
}
