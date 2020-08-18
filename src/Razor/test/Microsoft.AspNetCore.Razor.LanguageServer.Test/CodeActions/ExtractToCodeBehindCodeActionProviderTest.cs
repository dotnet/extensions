// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
{
    public class ExtractToCodeBehindCodeActionProviderTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_InvalidFileKind()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@page \"/test\"\n@code {}";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
            };

            var location = new SourceLocation(contents.IndexOf("code", StringComparison.Ordinal), -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents);
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new ExtractToCodeBehindCodeActionProvider();

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_OutsideCodeDirective()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@page \"/test\"\n@code {}";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
            };

            var location = new SourceLocation(contents.IndexOf("test", StringComparison.Ordinal), -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents);

            var provider = new ExtractToCodeBehindCodeActionProvider();

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_InCodeDirectiveBlock()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@page \"/test\"\n@code {}";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
            };

            var location = new SourceLocation(contents.IndexOf("code", StringComparison.Ordinal) + 6, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents);

            var provider = new ExtractToCodeBehindCodeActionProvider();

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_InCodeDirectiveMalformed()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@page \"/test\"\n@code";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
            };

            var location = new SourceLocation(contents.IndexOf("code", StringComparison.Ordinal), -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents);

            var provider = new ExtractToCodeBehindCodeActionProvider();

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_InCodeDirectiveWithMarkup()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@page \"/test\"\n@code { void Test() { <h1>Hello, world!</h1> } }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
            };

            var location = new SourceLocation(contents.IndexOf("code", StringComparison.Ordinal), -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents);

            var provider = new ExtractToCodeBehindCodeActionProvider();

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_InCodeDirective()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@page \"/test\"\n@code { private var x = 1; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
            };

            var location = new SourceLocation(contents.IndexOf("code", StringComparison.Ordinal), -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents);

            var provider = new ExtractToCodeBehindCodeActionProvider();

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Single(commandOrCodeActionContainer);
            var actionParams = commandOrCodeActionContainer
                .First().Command.Arguments[0]
                .ToObject<RazorCodeActionResolutionParams>().Data
                .ToObject<ExtractToCodeBehindCodeActionParams>();
            Assert.Equal(14, actionParams.RemoveStart);
            Assert.Equal(19, actionParams.ExtractStart);
            Assert.Equal(42, actionParams.ExtractEnd);
            Assert.Equal(42, actionParams.RemoveEnd);
        }

        [Fact]
        public async Task Handle_InFunctionsDirective()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@page \"/test\"\n@functions { private var x = 1; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
            };

            var location = new SourceLocation(contents.IndexOf("functions", StringComparison.Ordinal), -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents);

            var provider = new ExtractToCodeBehindCodeActionProvider();

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Single(commandOrCodeActionContainer);
            var actionParams = commandOrCodeActionContainer
                .First().Command.Arguments[0]
                .ToObject<RazorCodeActionResolutionParams>().Data
                .ToObject<ExtractToCodeBehindCodeActionParams>();
            Assert.Equal(14, actionParams.RemoveStart);
            Assert.Equal(24, actionParams.ExtractStart);
            Assert.Equal(47, actionParams.ExtractEnd);
            Assert.Equal(47, actionParams.RemoveEnd);
        }

        private static RazorCodeActionContext CreateRazorCodeActionContext(CodeActionParams request, SourceLocation location, string filePath, string text)
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            codeDocument.SetFileKind(FileKinds.Component);

            var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
            var options = RazorParserOptions.Create(o =>
            {
                o.Directives.Add(ComponentCodeDirective.Directive);
                o.Directives.Add(FunctionsDirective.Directive);
            });
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, options);
            codeDocument.SetSyntaxTree(syntaxTree);

            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(codeDocument.GetSourceText()));

            return new RazorCodeActionContext(request, documentSnapshot, codeDocument, location);
        }
    }
}
