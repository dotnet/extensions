// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
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
            long version = 1337;
            documentVersionCache.Setup(cache => cache.TryGetDocumentVersion(It.IsAny<DocumentSnapshot>(), out version))
                .Returns(true);

            DocumentVersionCache = documentVersionCache.Object;
            MappingService = new DefaultRazorDocumentMappingService();
        }

        private DocumentVersionCache DocumentVersionCache { get; }

        private RazorDocumentMappingService MappingService { get; }

        // These are more integration tests to validate that all the pieces work together
        [Fact]
        public async Task Handle_MapToDocumentRange_CSharp()
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
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorMapToDocumentRangeParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = new Range(new Position(0, 10), new Position(0, 22)),
                RazorDocumentUri = new Uri(documentPath),
            };
            var expectedRange = new Range(new Position(0, 4), new Position(0, 16));

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRange_CSharp_Unmapped()
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
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorMapToDocumentRangeParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = new Range(new Position(0, 0), new Position(0, 3)),
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageEndpoint.UndefinedRange, response.Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRange_CSharp_LeadingOverlapsUnmapped()
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
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorMapToDocumentRangeParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = new Range(new Position(0, 0), new Position(0, 22)),
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageEndpoint.UndefinedRange, response.Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRange_CSharp_TrailingOverlapsUnmapped()
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
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorMapToDocumentRangeParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = new Range(new Position(0, 10), new Position(0, 23)),
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageEndpoint.UndefinedRange, response.Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRange_Html()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now</p>");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorMapToDocumentRangeParams()
            {
                Kind = RazorLanguageKind.Html,
                ProjectedRange = new Range(new Position(0, 16), new Position(0, 20)),
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(request.ProjectedRange, response.Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRange_Razor()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now</p>");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorMapToDocumentRangeParams()
            {
                Kind = RazorLanguageKind.Razor,
                ProjectedRange = new Range(new Position(0, 3), new Position(0, 4)),
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(request.ProjectedRange, response.Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_MapToDocumentRange_Unsupported()
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
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorMapToDocumentRangeParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = new Range(new Position(0, 10), new Position(0, 22)),
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageEndpoint.UndefinedRange, response.Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ResolvesLanguageRequest_Razor()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("@{}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
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
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
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
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
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
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
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

        [Fact]
        public void GetLanguageKind_TagHelperElementOwnsName()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");
            descriptor.TagMatchingRule(rule => rule.TagName = "test");
            descriptor.SetTypeName("TestTagHelper");
            var text = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test>@Name</test>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text, new[] { descriptor.Build() });

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, 32 + Environment.NewLine.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKind_TagHelpersDoNotOwnTrailingEdge()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");
            descriptor.TagMatchingRule(rule => rule.TagName = "test");
            descriptor.SetTypeName("TestTagHelper");
            var text = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test></test>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text, new[] { descriptor.Build() });

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, 42 + Environment.NewLine.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Razor, languageKind);
        }

        [Fact]
        public void GetLanguageKind_TagHelperNestedCSharpAttribute()
        {
            // Arrange
            var descriptor = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly");
            descriptor.TagMatchingRule(rule => rule.TagName = "test");
            descriptor.BindAttribute(builder =>
            {
                builder.Name = "asp-int";
                builder.TypeName = typeof(int).FullName;
                builder.SetPropertyName("AspInt");
            });
            descriptor.SetTypeName("TestTagHelper");
            var text = $"@addTagHelper *, TestAssembly{Environment.NewLine}<test asp-int='123'></test>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text, new[] { descriptor.Build() });

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, 46 + Environment.NewLine.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKind_CSharp()
        {
            // Arrange
            var text = "<p>@Name</p>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, 5);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKind_Html()
        {
            // Arrange
            var text = "<p>Hello World</p>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, 5);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKind_DefaultsToRazorLanguageIfCannotLocateOwner()
        {
            // Arrange
            var text = "<p>Hello World</p>";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, text.Length + 1);

            // Assert
            Assert.Equal(RazorLanguageKind.Razor, languageKind);
        }

        [Fact]
        public void GetLanguageKind_HtmlEdgeEnd()
        {
            // Arrange
            var text = "Hello World";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKind_CSharpEdgeEnd()
        {
            // Arrange
            var text = "@Name";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKind_RazorEdgeWithCSharp()
        {
            // Arrange
            var text = "@{}";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, 2);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKind_RazorEdgeWithHtml()
        {
            // Arrange
            var text = "@{<br />}";
            var (classifiedSpans, tagHelperSpans) = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, tagHelperSpans, 2);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        private (IReadOnlyList<ClassifiedSpanInternal> classifiedSpans, IReadOnlyList<TagHelperSpanInternal> tagHelperSpans) GetClassifiedSpans(string text, IReadOnlyList<TagHelperDescriptor> tagHelpers = null)
        {
            var codeDocument = CreateCodeDocument(text, tagHelpers);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var classifiedSpans = syntaxTree.GetClassifiedSpans();
            var tagHelperSpans = syntaxTree.GetTagHelperSpans();
            return (classifiedSpans, tagHelperSpans);
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
