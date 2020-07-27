// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.CodeActions
{
    public class ComponentAccessibilityCodeActionProviderTest : LanguageServerTestBase
    {
        [Fact]
        public async Task Handle_InvalidFileKind()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "";
            var request = new CodeActionParams()
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
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 0), new Position(0, 0)),
            };

            var location = new SourceLocation(0, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(contents.IndexOf("Component"), 9));

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Null(commandOrCodeActionContainer);
        }

        [Fact]
        public async Task Handle_ExistingComponent()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "<Component></Component>";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 0), new Position(0, 0)),
            };

            var location = new SourceLocation(1, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(contents.IndexOf("Component"), 9));

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Equal(3, commandOrCodeActionContainer.Count());
            Assert.Equal("@using Fully.Qualified", commandOrCodeActionContainer.ElementAt(0).Command.Title);
            Assert.Equal("Fully.Qualified.Component", commandOrCodeActionContainer.ElementAt(1).CodeAction.Title);
            Assert.Equal("Create component from tag", commandOrCodeActionContainer.ElementAt(2).Command.Title) ;
        }

        [Fact]
        public async Task Handle_NewComponent()
        {
            // Arrange
            var documentPath = "c:/Test.razor";
            var contents = "<NewComponent></NewComponent>";
            var request = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier(new Uri(documentPath)),
                Range = new Range(new Position(0, 0), new Position(0, 0)),
            };

            var location = new SourceLocation(1, -1, -1);
            var context = CreateRazorCodeActionContext(request, location, documentPath, contents, new SourceSpan(contents.IndexOf("Component"), 9));

            var provider = new ComponentAccessibilityCodeActionProvider(new DefaultTagHelperFactsService(), FilePathNormalizer);

            // Act
            var commandOrCodeActionContainer = await provider.ProvideAsync(context, default);

            // Assert
            Assert.Single(commandOrCodeActionContainer);
            Assert.Equal("Create component from tag", commandOrCodeActionContainer.ElementAt(0).Command.Title);
        }

        private static RazorCodeActionContext CreateRazorCodeActionContext(CodeActionParams request, SourceLocation location, string filePath, string text, SourceSpan componentSourceSpan)
        {
            var shortComponent = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "Fully.Qualified.Component", "TestAssembly");
            shortComponent.TagMatchingRule(rule => rule.TagName = "Component");
            var fullyQualifiedComponent = TagHelperDescriptorBuilder.Create(ComponentMetadata.Component.TagHelperKind, "Fully.Qualified.Component", "TestAssembly");
            fullyQualifiedComponent.TagMatchingRule(rule => rule.TagName = "Fully.Qualified.Component");

            var tagHelpers = new[] { shortComponent.Build(), fullyQualifiedComponent.Build() };

            var sourceDocument = TestRazorSourceDocument.Create(text, filePath: filePath, relativePath: filePath);
            var projectEngine = RazorProjectEngine.Create(builder => {
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
                document.Project.TagHelpers == tagHelpers);

            return new RazorCodeActionContext(request, documentSnapshot, codeDocument, location);
        }
    }
}
