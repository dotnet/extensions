// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    public class DefaultCSharpCodeActionProviderTest : LanguageServerTestBase
    {
        private readonly RazorCodeAction[] SupportedCodeActions;

        public DefaultCSharpCodeActionProviderTest()
        {
            SupportedCodeActions = DefaultCSharpCodeActionProvider
                .SupportedDefaultCodeActionNames
                .Select(name => new RazorCodeAction() { Name = name })
                .ToArray();
        }

        [Fact]
        public async Task ProvideAsync_ValidCodeActions_ReturnsProvidedCodeAction()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@code { Path; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
            };

            var location = new SourceLocation(8, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(8, 4));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new DefaultCSharpCodeActionProvider();

            // Act
            var providedCodeActions = await provider.ProvideAsync(context, SupportedCodeActions, default);

            // Assert
            Assert.Equal(SupportedCodeActions.Length, providedCodeActions.Count);
            var providedNames = providedCodeActions.Select(action => action.Name);
            var expectedNames = SupportedCodeActions.Select(action => action.Name);
            Assert.Equal(expectedNames, providedNames);
        }

        [Fact]
        public async Task ProvideAsync_SupportsCodeActionResolveFalse_ValidCodeActions_ReturnsEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@code { Path; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
            };

            var location = new SourceLocation(8, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(8, 4), supportsCodeActionResolve: false);
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new DefaultCSharpCodeActionProvider();

            // Act
            var providedCodeActions = await provider.ProvideAsync(context, SupportedCodeActions, default);

            // Assert
            Assert.Empty(providedCodeActions);
        }

        [Fact]
        public async Task ProvideAsync_FunctionsBlock_ValidCodeActions_ReturnsEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@functions { Path; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
            };

            var location = new SourceLocation(13, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(13, 4));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new DefaultCSharpCodeActionProvider();

            // Act
            var providedCodeActions = await provider.ProvideAsync(context, SupportedCodeActions, default);

            // Assert
            Assert.Empty(providedCodeActions);
        }

        [Fact]
        public async Task ProvideAsync_InvalidCodeActions_ReturnsNoCodeActions()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "@code { Path; }";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
                Context = new CodeActionContext()
            };

            var location = new SourceLocation(8, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(8, 4));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new DefaultCSharpCodeActionProvider();

            var codeActions = new RazorCodeAction[]
            {
               new RazorCodeAction()
               {
                   Title = "Do something not really supported in razor",
                   Name = "Non-existant name"
               }
            };

            // Act
            var providedCodeActions = await provider.ProvideAsync(context, codeActions, default);

            // Assert
            Assert.Empty(providedCodeActions);
        }

        private static RazorCodeActionContext CreateRazorCodeActionContext(
            CodeActionParams request,
            SourceLocation location,
            string filePath,
            string text,
            SourceSpan componentSourceSpan,
            bool supportsFileCreation = true,
            bool supportsCodeActionResolve = true)
        {
            var tagHelpers = Array.Empty<TagHelperDescriptor>();
            var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
            var projectEngine = RazorProjectEngine.Create(builder =>
            {
                builder.AddTagHelpers(tagHelpers);
            });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, FileKinds.Component, Array.Empty<RazorSourceDocument>(), tagHelpers);

            var cSharpDocument = codeDocument.GetCSharpDocument();
            var diagnosticDescriptor = new RazorDiagnosticDescriptor("RZ10012", () => "", RazorDiagnosticSeverity.Error);
            var diagnostic = RazorDiagnostic.Create(diagnosticDescriptor, componentSourceSpan);
            var cSharpDocumentWithDiagnostic = RazorCSharpDocument.Create(cSharpDocument.GeneratedCode, cSharpDocument.Options, new[] { diagnostic });
            codeDocument.SetCSharpDocument(cSharpDocumentWithDiagnostic);

            var documentSnapshot = Mock.Of<DocumentSnapshot>(document =>
                document.GetGeneratedOutputAsync() == Task.FromResult(codeDocument) &&
                document.GetTextAsync() == Task.FromResult(codeDocument.GetSourceText()) &&
                document.Project.TagHelpers == tagHelpers, MockBehavior.Strict);

            var sourceText = SourceText.From(text);

            var context = new RazorCodeActionContext(request, documentSnapshot, codeDocument, location, sourceText, supportsFileCreation, supportsCodeActionResolve);

            return context;
        }
    }
}
