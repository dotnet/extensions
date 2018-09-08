// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorDocumentSynchronizationEndpointTest : TestBase
    {
        private DocumentResolver DocumentResolver => Mock.Of<DocumentResolver>();

        private RazorProjectService ProjectService => Mock.Of<RazorProjectService>();

        private RemoteTextLoaderFactory TextLoaderFactory => Mock.Of<RemoteTextLoaderFactory>(factory => factory.Create(It.IsAny<string>()) == Mock.Of<TextLoader>());

        [Fact]
        public void ApplyContentChanges_SingleChange()
        {
            // Arrange
            var endpoint = new RazorDocumentSynchronizationEndpoint(Dispatcher, DocumentResolver, TextLoaderFactory, ProjectService, Logger);
            var sourceText = SourceText.From("Hello World");
            var change = new TextDocumentContentChangeEvent()
            {
                Range = new Range(new Position(0, 5), new Position(0, 5)),
                RangeLength = 0,
                Text = "!"
            };

            // Act
            var result = endpoint.ApplyContentChanges(new[] { change }, sourceText);

            // Assert
            var resultString = GetString(result);
            Assert.Equal("Hello! World", resultString);
        }

        [Fact]
        public void ApplyContentChanges_MultipleChanges()
        {
            // Arrange
            var endpoint = new RazorDocumentSynchronizationEndpoint(Dispatcher, DocumentResolver, TextLoaderFactory, ProjectService, Logger);
            var sourceText = SourceText.From("Hello World");
            var changes = new[] {
                new TextDocumentContentChangeEvent()
                {
                    Range = new Range(new Position(0, 5), new Position(0, 5)),
                    RangeLength = 0,
                    Text = Environment.NewLine
                },
                // Hello
                //  World

                new TextDocumentContentChangeEvent()
                {
                    Range = new Range(new Position(1, 0), new Position(1, 0)),
                    RangeLength = 0,
                    Text = "!"
                },
                // Hello
                // ! World

                new TextDocumentContentChangeEvent()
                {
                    Range = new Range(new Position(0, 1), new Position(0, 1)),
                    RangeLength = 4,
                    Text = "i!" + Environment.NewLine
                },
                // Hi!
                //
                // ! World
            };

            // Act
            var result = endpoint.ApplyContentChanges(changes, sourceText);

            // Assert
            var resultString = GetString(result);
            Assert.Equal(@"Hi!

! World", resultString);
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_DidChangeTextDocument_UpdatesDocument()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var sourceText = SourceText.From("<p>");
            var documentResolver = CreateDocumentResolver(documentPath, sourceText);
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.UpdateDocument(It.IsAny<string>(), It.IsAny<SourceText>(), It.IsAny<long>()))
                .Callback<string, SourceText, long>((path, text, version) =>
                {
                    var resultString = GetString(text);
                    Assert.Equal("<p></p>", resultString);
                    Assert.Equal(documentPath, path);
                    Assert.Equal(1337, version);
                });
            var endpoint = new RazorDocumentSynchronizationEndpoint(Dispatcher, documentResolver, TextLoaderFactory, projectService.Object, Logger);
            var change = new TextDocumentContentChangeEvent()
            {
                Range = new Range(new Position(0, 3), new Position(0, 3)),
                RangeLength = 0,
                Text = "</p>"
            };
            var request = new DidChangeTextDocumentParams()
            {
                ContentChanges = new Container<TextDocumentContentChangeEvent>(change),
                TextDocument = new VersionedTextDocumentIdentifier()
                {
                    Uri = new Uri(documentPath),
                    Version = 1337,
                }
            };

            // Act
            await Task.Run(() => endpoint.Handle(request, default));

            // Assert
            projectService.VerifyAll();
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_DidOpenTextDocument_AddsDocument()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.OpenDocument(It.IsAny<string>(), It.IsAny<SourceText>(), It.IsAny<long>()))
                .Callback<string, SourceText, long>((path, text, version) =>
                {
                    var resultString = GetString(text);
                    Assert.Equal("hello", resultString);
                    Assert.Equal(documentPath, path);
                    Assert.Equal(1337, version);
                });
            var endpoint = new RazorDocumentSynchronizationEndpoint(Dispatcher, DocumentResolver, TextLoaderFactory, projectService.Object, Logger);
            var request = new DidOpenTextDocumentParams()
            {
                TextDocument = new TextDocumentItem()
                {
                    Text = "hello",
                    Uri = new Uri(documentPath),
                    Version = 1337,
                }
            };

            // Act
            await Task.Run(() => endpoint.Handle(request, default));

            // Assert
            projectService.VerifyAll();
        }

        // This is more of an integration test to validate that all the pieces work together
        [Fact]
        public async Task Handle_DidCloseTextDocument_ClosesDocument()
        {
            // Arrange
            var documentPath = "C:/path/to/document.cshtml";
            var projectService = new Mock<RazorProjectService>(MockBehavior.Strict);
            projectService.Setup(service => service.CloseDocument(It.IsAny<string>(), It.IsAny<TextLoader>()))
                .Callback<string, TextLoader>((path, loader) =>
                {
                    Assert.Equal(documentPath, path);
                    Assert.NotNull(loader);
                });
            var endpoint = new RazorDocumentSynchronizationEndpoint(Dispatcher, DocumentResolver, TextLoaderFactory, projectService.Object, Logger);
            var request = new DidCloseTextDocumentParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = new Uri(documentPath)
                }
            };

            // Act
            await Task.Run(() => endpoint.Handle(request, default));

            // Assert
            projectService.VerifyAll();
        }

        private string GetString(SourceText sourceText)
        {
            var sourceChars = new char[sourceText.Length];
            sourceText.CopyTo(0, sourceChars, 0, sourceText.Length);
            var sourceString = new string(sourceChars);

            return sourceString;
        }

        private static DocumentResolver CreateDocumentResolver(string documentPath, SourceText sourceText)
        {
            var documentSnapshot = Mock.Of<DocumentSnapshot>(document => document.GetTextAsync() == Task.FromResult(sourceText) && document.FilePath == documentPath);
            var documentResolver = new Mock<DocumentResolver>();
            documentResolver.Setup(resolver => resolver.TryResolveDocument(documentPath, out documentSnapshot))
                .Returns(true);
            return documentResolver.Object;
        }
    }
}
