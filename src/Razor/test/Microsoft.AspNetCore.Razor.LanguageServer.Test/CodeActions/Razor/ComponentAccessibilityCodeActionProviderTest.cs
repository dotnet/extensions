// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor.Razor;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    public class ComponentAccessibilityCodeActionProviderTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_InvalidSyntaxTree_NoStartNode()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "";
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(),
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(0, 0));
            context.CodeDocument.SetFileKind(FileKinds.Legacy);

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_CursorOutsideComponent()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = " <Component></Component>";
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 0), new Position(0, 0)),
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(contents.IndexOf("Component", StringComparison.Ordinal), 9));

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_ExistingComponent_SupportsFileCreationTrue_ReturnsResults()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "<Component></Component>";
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 0), new Position(0, 0)),
            };

            var location = new SourceLocation(1, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(contents.IndexOf("Component", StringComparison.Ordinal), 9), supportsFileCreation: true);

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Collection(commandOrCodeActionContainer,
                e =>
                {
                    Assert.Equal("@using Fully.Qualified", e.Title);
                    Assert.NotNull(e.Data);
                    Assert.Null(e.Edit);
                },
                e =>
                {
                    Assert.Equal("Fully.Qualified.Component", e.Title);
                    Assert.NotNull(e.Edit);
                    Assert.NotNull(e.Edit.DocumentChanges);
                    Assert.Null(e.Data);
                },
                e =>
                {
                    Assert.Equal("Create component from tag", e.Title);
                    Assert.NotNull(e.Data);
                    Assert.Null(e.Edit);
                });
        }

        [Fact]
        public async Task Handle_NewComponent_SupportsFileCreationTrue_ReturnsResult()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "<NewComponent></NewComponent>";
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 0), new Position(0, 0)),
            };

            var location = new SourceLocation(1, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(contents.IndexOf("Component", StringComparison.Ordinal), 9), supportsFileCreation: true);

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            var command = Assert.Single(commandOrCodeActionContainer);
            Assert.Equal("Create component from tag", command.Title);
            Assert.NotNull(command.Data);
        }

        [Fact]
        public async Task Handle_NewComponent_SupportsFileCreationFalse_ReturnsEmpty()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "<NewComponent></NewComponent>";
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 0), new Position(0, 0)),
            };

            var location = new SourceLocation(1, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(contents.IndexOf("Component", StringComparison.Ordinal), 9), supportsFileCreation: false);

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Empty(commandOrCodeActionContainer);
        }


        [Fact]
        public async Task Handle_ExistingComponent_SupportsFileCreationFalse_ReturnsResults()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "<Component></Component>";
            var request = new RazorCodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 0), new Position(0, 0)),
            };

            var location = new SourceLocation(1, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(contents.IndexOf("Component", StringComparison.Ordinal), 9), supportsFileCreation: false);

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Collection(commandOrCodeActionContainer,
                e =>
                {
                    Assert.Equal("@using Fully.Qualified", e.Title);
                    Assert.NotNull(e.Data);
                    Assert.Null(e.Edit);
                },
                e =>
                {
                    Assert.Equal("Fully.Qualified.Component", e.Title);
                    Assert.NotNull(e.Edit);
                    Assert.NotNull(e.Edit.DocumentChanges);
                    Assert.Null(e.Data);
                });
        }

        private static RazorCodeActionContext CreateRazorCodeActionContext(RazorCodeActionParams request, SourceLocation location, string filePath, string text, SourceSpan componentSourceSpan, bool supportsFileCreation = true)
        {
            var shortComponent = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "Fully.Qualified.Component", "TestAssembly");
            shortComponent.TagMatchingRule(rule => rule.TagName = "Component");
            var fullyQualifiedComponent = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "Fully.Qualified.Component", "TestAssembly");
            fullyQualifiedComponent.TagMatchingRule(rule => rule.TagName = "Fully.Qualified.Component");

            var tagHelpers = new[] { shortComponent.Build(), fullyQualifiedComponent.Build() };

            var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
            var projectEngine = RazorProjectEngine.Create(builder => builder.AddTagHelpers(tagHelpers));
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

            var context = new RazorCodeActionContext(request, documentSnapshot, codeDocument, location, sourceText, supportsFileCreation, supportsCodeActionResolve: true);

            return context;
        }
    }
}
