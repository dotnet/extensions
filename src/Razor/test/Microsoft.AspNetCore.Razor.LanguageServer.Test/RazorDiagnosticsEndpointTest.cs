// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
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
    public class RazorDiagnosticsEndpointTest : LanguageServerTestBase
    {
        public RazorDiagnosticsEndpointTest()
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

        [Fact(Skip = "Debug.Fail doesn't work in CI")]
        public async Task Handle_DocumentResolveFailed_ThrowsDebugFail()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var documentResolver = new Mock<DocumentResolver>();
            var documentSnapshot = default(DocumentSnapshot);
            documentResolver.Setup(resolver => resolver.TryResolveDocument(documentPath, out documentSnapshot))
                .Returns(false);

            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver.Object, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () => await Task.Run(() => diagnosticsEndpoint.Handle(request, default)));
        }

        [Fact]
        public async Task Handle_DocumentVersionFailed_ReturnsNullDiagnostics()
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
            var documentVersionCache = new Mock<DocumentVersionCache>();
            int? version = default;
            documentVersionCache.Setup(cache => cache.TryGetDocumentVersion(It.IsAny<DocumentSnapshot>(), out version))
                .Returns(false);

            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, documentVersionCache.Object, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] { new Diagnostic() { Range = new Range(new Position(0, 10), new Position(0, 22)) } },
                RazorDocumentUri = new Uri(documentPath),
            };
            var expectedRange = new Range(new Position(0, 4), new Position(0, 16));

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics[0].Range);
            Assert.Null(response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharp()
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
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] { new Diagnostic() { Range = new Range(new Position(0, 10), new Position(0, 22)) } },
                RazorDocumentUri = new Uri(documentPath),
            };
            var expectedRange = new Range(new Position(0, 4), new Position(0, 16));

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics[0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_FilterDiagnostics_CSharpWarning()
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
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new Diagnostic() {
                        Range = new Range(new Position(0, 10), new Position(0, 22)),
                        Code = RazorDiagnosticsEndpoint.DiagnosticsToIgnore.First(),
                        Severity = DiagnosticSeverity.Warning
                    }
                },
                RazorDocumentUri = new Uri(documentPath),
            };
            var expectedRange = new Range(new Position(0, 4), new Position(0, 16));

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_DoNotFilterErrorDiagnostics_CSharpError()
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
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new Diagnostic() {
                        Range = new Range(new Position(0, 10), new Position(0, 22)),
                        Code = RazorDiagnosticsEndpoint.DiagnosticsToIgnore.First(),
                        Severity = DiagnosticSeverity.Error
                    }
                },
                RazorDocumentUri = new Uri(documentPath),
            };
            var expectedRange = new Range(new Position(0, 4), new Position(0, 16));

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(expectedRange, response.Diagnostics[0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharpWarning_Unmapped_ReturnsNoDiagnostics()
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
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new Diagnostic() {
                        Severity = DiagnosticSeverity.Warning,
                        Range = new Range(new Position(0, 0), new Position(0, 3))
                    }
                },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_CSharpError_Unmapped_ReturnsUndefinedRangeDiagnostic()
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
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] {
                    new Diagnostic() {
                        Severity = DiagnosticSeverity.Error,
                        Range = new Range(new Position(0, 0), new Position(0, 3))
                    }
                },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RangeExtensions.UndefinedRange, response.Diagnostics[0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Html()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now</p>");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] { new Diagnostic() { Range = new Range(new Position(0, 16), new Position(0, 20)) } },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(request.Diagnostics[0].Range, response.Diagnostics[0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Razor()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("<p>@DateTime.Now</p>");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Razor,
                Diagnostics = new[] { new Diagnostic() { Range = new Range(new Position(0, 3), new Position(0, 4)) } },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(request.Diagnostics[0].Range, response.Diagnostics[0].Range);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_Unsupported()
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
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.CSharp,
                Diagnostics = new[] { new Diagnostic() { Range = new Range(new Position(0, 10), new Position(0, 22)) } },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_HTML_WithCSharpInAttribute_SingleDiagnostic()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p style=\"padding: @System.Math.PI px;\">Hello</p>",
                "var __o = System.Math.PI",
                new[] {
                    new SourceMapping(
                        new SourceSpan(20, 14),
                        new SourceSpan(10, 14))
                });
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[] { new Diagnostic() { Range = new Range(new Position(0, 18), new Position(0, 19)) } },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_HTML_WithCSharpInAttribute_MultipleDiagnostics()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                "<p style=\"abc;padding: @System.Math.PI px;abc;\">Hello</p>",
                "var __o = System.Math.PI",
                new[] {
                    new SourceMapping(
                        new SourceSpan(24, 14),
                        new SourceSpan(10, 14))
                });
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new Diagnostic() { Range = new Range(new Position(0, 1), new Position(0, 2)) },     // start of `p` tag
                    new Diagnostic() { Range = new Range(new Position(0, 13), new Position(0, 14)) },   // leading `abc`
                    new Diagnostic() { Range = new Range(new Position(0, 25), new Position(0, 26)) },   // `@`
                    new Diagnostic() { Range = new Range(new Position(0, 45), new Position(0, 46)) },   // trailing `abc`
                    new Diagnostic() { Range = new Range(new Position(0, 55), new Position(0, 57)) }    // in `Hello`
                },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Collection(response.Diagnostics,
                d => Assert.Equal(new Range(new Position(0, 1), new Position(0, 2)), d.Range),
                d => Assert.Equal(new Range(new Position(0, 55), new Position(0, 57)), d.Range));
            Assert.Equal(1337, response.HostDocumentVersion);
        }

        [Fact]
        public async Task Handle_ProcessDiagnostics_HTML_WithCSharpInTagHelperAttribute_MultipleDiagnostics()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";

            var descriptor = TagHelperDescriptorBuilder.Create("ButtonTagHelper", "TestAssembly");
            descriptor.SetTypeName("TestNamespace.ButtonTagHelper");
            descriptor.TagMatchingRule(builder => builder.RequireTagName("button"));
            descriptor.BindAttribute(builder =>
                builder
                    .Name("onactivate")
                    .PropertyName("onactivate")
                    .TypeName(typeof(string).FullName));

            var addTagHelper = $"@addTagHelper *, TestAssembly{Environment.NewLine}";
            var codeDocument = CreateCodeDocumentWithCSharpProjection(
                $"{addTagHelper}<button onactivate=\"Send\" disabled=\"@(Something)\">Hi</button>",
                $"var __o = Send;var __o = Something;",
                new[] {
                    new SourceMapping(
                        new SourceSpan(addTagHelper.Length + 20, 4),
                        new SourceSpan(addTagHelper.Length + 10, 4)),

                    new SourceMapping(
                        new SourceSpan(addTagHelper.Length + 38, 9),
                        new SourceSpan(addTagHelper.Length + 25, 9))
                },
                new[] {
                    descriptor.Build()
                });
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var diagnosticsEndpoint = new RazorDiagnosticsEndpoint(Dispatcher, documentResolver, DocumentVersionCache, MappingService, LoggerFactory);
            var request = new RazorDiagnosticsParams()
            {
                Kind = RazorLanguageKind.Html,
                Diagnostics = new[]
                {
                    new Diagnostic() { Range = new Range(new Position(0, addTagHelper.Length + 20), new Position(0, addTagHelper.Length + 25)) },
                    new Diagnostic() { Range = new Range(new Position(0, addTagHelper.Length + 38), new Position(0, addTagHelper.Length + 47)) }
                },
                RazorDocumentUri = new Uri(documentPath),
            };

            // Act
            var response = await Task.Run(() => diagnosticsEndpoint.Handle(request, default));

            // Assert
            Assert.Empty(response.Diagnostics);
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
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, FileKinds.Legacy, Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
        }

        private static RazorCodeDocument CreateCodeDocumentWithCSharpProjection(
            string razorSource,
            string projectedCSharpSource,
            IEnumerable<SourceMapping> sourceMappings,
            IReadOnlyList<TagHelperDescriptor> tagHelpers = null)
        {
            var codeDocument = CreateCodeDocument(razorSource, tagHelpers);
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
