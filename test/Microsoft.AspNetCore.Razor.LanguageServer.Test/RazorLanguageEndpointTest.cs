// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.VisualStudio.Editor.Razor;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorLanguageEndpointTest : TestBase
    {
        public RazorLanguageEndpointTest()
        {
            // Working around strong naming restriction.
            var syntaxFactsType = Assembly
                .Load("Microsoft.VisualStudio.Editor.Razor")
                .GetType("Microsoft.VisualStudio.Editor.Razor.DefaultRazorSyntaxFactsService");
            SyntaxFactsService = (RazorSyntaxFactsService)Activator.CreateInstance(syntaxFactsType);
        }

        public RazorSyntaxFactsService SyntaxFactsService { get; }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_ResolvesLanguageRequest_Razor()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("@{}");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, SyntaxFactsService, Logger);
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
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_ResolvesLanguageRequest_Html()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("<s");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, SyntaxFactsService, Logger);
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
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_ResolvesLanguageRequest_CSharp()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var codeDocument = CreateCodeDocument("@");
            var documentResolver = CreateDocumentResolver(documentPath, codeDocument);
            var languageEndpoint = new RazorLanguageEndpoint(Dispatcher, documentResolver, SyntaxFactsService, Logger);
            var request = new RazorLanguageQueryParams()
            {
                Uri = new Uri(documentPath),
                Position = new Position(0, 1),
            };

            // Act
            var response = await Task.Run(() => languageEndpoint.Handle(request, default));

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, response.Kind);

            // NOTE: This will change once C# gets remapped.
            Assert.Equal(request.Position, response.Position);
        }

        [Fact]
        public void GetLanguageKind_CSharp()
        {
            // Arrange
            var text = "<p>@Name</p>";
            var classifiedSpans = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, 5);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKind_Html()
        {
            // Arrange
            var text = "<p>Hello World</p>";
            var classifiedSpans = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, 5);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKind_DefaultsToRazorLanguageIfCannotLocateOwner()
        {
            // Arrange
            var text = "<p>Hello World</p>";
            var classifiedSpans = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, text.Length + 1);

            // Assert
            Assert.Equal(RazorLanguageKind.Razor, languageKind);
        }

        [Fact]
        public void GetLanguageKind_HtmlEdgeEnd()
        {
            // Arrange
            var text = "Hello World";
            var classifiedSpans = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        [Fact]
        public void GetLanguageKind_CSharpEdgeEnd()
        {
            // Arrange
            var text = "@Name";
            var classifiedSpans = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, text.Length);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKind_RazorEdgeWithCSharp()
        {
            // Arrange
            var text = "@{}";
            var classifiedSpans = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, 2);

            // Assert
            Assert.Equal(RazorLanguageKind.CSharp, languageKind);
        }

        [Fact]
        public void GetLanguageKind_RazorEdgeWithHtml()
        {
            // Arrange
            var text = "@{<br />}";
            var classifiedSpans = GetClassifiedSpans(text);

            // Act
            var languageKind = RazorLanguageEndpoint.GetLanguageKind(classifiedSpans, 2);

            // Assert
            Assert.Equal(RazorLanguageKind.Html, languageKind);
        }

        public IReadOnlyList<ClassifiedSpan> GetClassifiedSpans(string text)
        {
            var codeDocument = CreateCodeDocument(text);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var classifiedSpans = SyntaxFactsService.GetClassifiedSpans(syntaxTree);
            return classifiedSpans;
        }

        private static DocumentResolver CreateDocumentResolver(string documentPath, RazorCodeDocument codeDocument)
        {
            var documentSnapshot = Mock.Of<DocumentSnapshotShim>(document => document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument));
            var documentResolver = new Mock<DocumentResolver>();
            documentResolver.Setup(resolver => resolver.TryResolveDocument(documentPath, out documentSnapshot))
                .Returns(true);
            return documentResolver.Object;
        }

        private static RazorCodeDocument CreateCodeDocument(string text)
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument);
            codeDocument.SetSyntaxTree(syntaxTree);
            return codeDocument;
        }
    }
}
