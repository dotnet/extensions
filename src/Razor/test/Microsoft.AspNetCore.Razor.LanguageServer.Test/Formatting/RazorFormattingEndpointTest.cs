// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Options;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class RazorFormattingEndpointTest : LanguageServerTestBase
    {
        public RazorFormattingEndpointTest()
        {
            EmptyDocumentResolver = Mock.Of<DocumentResolver>();
        }

        private DocumentResolver EmptyDocumentResolver { get; }

        [Fact]
        public async Task Handle_FormattingEnabled_InvokesFormattingService()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var uri = new Uri("file://path/test.razor");
            var documentResolver = CreateDocumentResolver(uri.GetAbsoluteOrUNCPath(), codeDocument);
            var formattingService = new TestRazorFormattingService();
            var optionsMonitor = GetOptionsMonitor(enableFormatting: true);
            var endpoint = new RazorFormattingEndpoint(Dispatcher, documentResolver, formattingService, optionsMonitor, LoggerFactory);
            var @params = new DocumentRangeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier(uri)
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(formattingService.Called);
        }

        [Fact]
        public async Task Handle_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var formattingService = new TestRazorFormattingService();
            var optionsMonitor = GetOptionsMonitor(enableFormatting: true);
            var endpoint = new RazorFormattingEndpoint(Dispatcher, EmptyDocumentResolver, formattingService, optionsMonitor, LoggerFactory);
            var uri = new Uri("file://path/test.razor");
            var @params = new DocumentRangeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier(uri)
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_UnsupportedCodeDocument_ReturnsNull()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            codeDocument.SetUnsupported();
            var uri = new Uri("file://path/test.razor");
            var documentResolver = CreateDocumentResolver(uri.AbsolutePath, codeDocument);
            var formattingService = new TestRazorFormattingService();
            var optionsMonitor = GetOptionsMonitor(enableFormatting: true);
            var endpoint = new RazorFormattingEndpoint(Dispatcher, documentResolver, formattingService, optionsMonitor, LoggerFactory);
            var @params = new DocumentRangeFormattingParams()
            {
                TextDocument = new TextDocumentIdentifier(uri)
            };

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task Handle_FormattingDisabled_ReturnsNull()
        {
            // Arrange
            var formattingService = new TestRazorFormattingService();
            var optionsMonitor = GetOptionsMonitor(enableFormatting: false);
            var endpoint = new RazorFormattingEndpoint(Dispatcher, EmptyDocumentResolver, formattingService, optionsMonitor, LoggerFactory);
            var @params = new DocumentRangeFormattingParams();

            // Act
            var result = await endpoint.Handle(@params, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        private static IOptionsMonitor<RazorLSPOptions> GetOptionsMonitor(bool enableFormatting)
        {
            var monitor = new Mock<IOptionsMonitor<RazorLSPOptions>>();
            monitor.SetupGet(m => m.CurrentValue).Returns(new RazorLSPOptions(default, enableFormatting, true));
            return monitor.Object;
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

        private class TestRazorFormattingService : RazorFormattingService
        {
            public bool Called { get; private set; }

            public override Task<TextEdit[]> FormatAsync(Uri uri, RazorCodeDocument codeDocument, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range, FormattingOptions options)
            {
                Called = true;
                return Task.FromResult(Array.Empty<TextEdit>());
            }
        }
    }
}
