// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class FormattingTestBase : LanguageServerTestBase
    {
        protected async Task RunFormattingTestAsync(string input, string expected, int tabSize = 4, bool insertSpaces = true, string fileKind = null)
        {
            // Arrange
            fileKind ??= FileKinds.Component;
            var start = input.IndexOf('|', StringComparison.Ordinal);
            var end = input.LastIndexOf('|');
            input = input.Replace("|", string.Empty, StringComparison.Ordinal);

            var source = SourceText.From(input);
            var span = TextSpan.FromBounds(start, end - 1);
            var range = span.AsRange(source);

            var path = "file:///path/to/document.razor";
            var uri = new Uri(path);
            var (codeDocument, documentSnapshot) = CreateCodeDocumentAndSnapshot(source, uri.AbsolutePath, fileKind: fileKind);
            var options = new FormattingOptions()
            {
                TabSize = tabSize,
                InsertSpaces = insertSpaces,
            };

            var formattingService = CreateFormattingService(codeDocument);

            // Act
            var edits = await formattingService.FormatAsync(uri, documentSnapshot, range, options, CancellationToken.None);

            // Assert
            var edited = ApplyEdits(source, edits);
            var actual = edited.ToString();
            Assert.Equal(expected, actual);
        }

        protected async Task RunOnTypeFormattingTestAsync(string input, string expected, string triggerCharacter, int tabSize = 4, bool insertSpaces = true, string fileKind = null)
        {
            // Arrange
            fileKind ??= FileKinds.Component;
            var beforeTrigger = input.IndexOf('|', StringComparison.Ordinal);
            var afterTrigger = input.LastIndexOf('|') - 1;
            input = input.Replace("|", string.Empty, StringComparison.Ordinal);

            var source = SourceText.From(input);

            var path = "file:///path/to/document.razor";
            var uri = new Uri(path);
            var (codeDocument, documentSnapshot) = CreateCodeDocumentAndSnapshot(source, uri.AbsolutePath, fileKind: fileKind);
            var options = new FormattingOptions()
            {
                TabSize = tabSize,
                InsertSpaces = insertSpaces,
            };

            var formattingService = CreateFormattingService(codeDocument);
            var (kind, projectedEdits) = GetFormattedEdits(codeDocument, expected, beforeTrigger);

            // Act
            var edits = await formattingService.ApplyFormattedEditsAsync(uri, documentSnapshot, kind, projectedEdits, options, CancellationToken.None);

            // Assert
            var edited = ApplyEdits(source, edits);
            var actual = edited.ToString();
            Assert.Equal(expected, actual);
        }

        private (RazorLanguageKind, TextEdit[]) GetFormattedEdits(RazorCodeDocument codeDocument, string expected, int positionBeforeTriggerChar)
        {
            var mappingService = new DefaultRazorDocumentMappingService();
            var languageKind = mappingService.GetLanguageKind(codeDocument, positionBeforeTriggerChar);

            var expectedText = SourceText.From(expected);
            var (expectedCodeDocument, _) = CreateCodeDocumentAndSnapshot(expectedText, codeDocument.Source.FilePath, fileKind: codeDocument.GetFileKind());

            var edits = Array.Empty<TextEdit>();

            if (languageKind == RazorLanguageKind.CSharp)
            {
                var beforeCSharpText = SourceText.From(codeDocument.GetCSharpDocument().GeneratedCode);
                var afterCSharpText = SourceText.From(expectedCodeDocument.GetCSharpDocument().GeneratedCode);
                edits = SourceTextDiffer.GetMinimalTextChanges(beforeCSharpText, afterCSharpText, lineDiffOnly: false).Select(c => c.AsTextEdit(beforeCSharpText)).ToArray();
            }
            else if (languageKind == RazorLanguageKind.Html)
            {
                var beforeHtmlText = SourceText.From(codeDocument.GetHtmlDocument().GeneratedHtml);
                var afterHtmlText = SourceText.From(expectedCodeDocument.GetHtmlDocument().GeneratedHtml);
                edits = SourceTextDiffer.GetMinimalTextChanges(beforeHtmlText, afterHtmlText, lineDiffOnly: false).Select(c => c.AsTextEdit(beforeHtmlText)).ToArray();
            }

            return (languageKind, edits);
        }

        private RazorFormattingService CreateFormattingService(RazorCodeDocument codeDocument)
        {
            var mappingService = new DefaultRazorDocumentMappingService();

            var client = new FormattingLanguageServerClient();
            client.AddCodeDocument(codeDocument);
            var passes = new List<IFormattingPass>()
            {
                new CodeBlockDirectiveFormattingPass(mappingService, FilePathNormalizer, client, LoggerFactory),
                new CSharpOnTypeFormattingPass(mappingService, FilePathNormalizer, client, LoggerFactory),
                new FormattingStructureValidationPass(mappingService, FilePathNormalizer, client, LoggerFactory),
                new FormattingContentValidationPass(mappingService, FilePathNormalizer, client, LoggerFactory),
            };

            return new DefaultRazorFormattingService(passes, LoggerFactory);
        }

        private SourceText ApplyEdits(SourceText source, TextEdit[] edits)
        {
            var changes = edits.Select(e => e.AsTextChange(source));
            return source.WithChanges(changes);
        }

        private static (RazorCodeDocument, DocumentSnapshot) CreateCodeDocumentAndSnapshot(SourceText text, string path, IReadOnlyList<TagHelperDescriptor> tagHelpers = null, string fileKind = default)
        {
            fileKind ??= FileKinds.Component;
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
            var sourceDocument = text.GetRazorSourceDocument(path, path);
            var projectEngine = RazorProjectEngine.Create(builder => { builder.SetRootNamespace("Test"); });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, Array.Empty<RazorSourceDocument>(), tagHelpers);

            var documentSnapshot = new Mock<DocumentSnapshot>();
            documentSnapshot.Setup(d => d.GetGeneratedOutputAsync()).Returns(Task.FromResult(codeDocument));
            documentSnapshot.Setup(d => d.Project.GetProjectEngine()).Returns(projectEngine);
            documentSnapshot.Setup(d => d.TargetPath).Returns(path);
            documentSnapshot.Setup(d => d.Project.TagHelpers).Returns(tagHelpers);
            documentSnapshot.Setup(d => d.FileKind).Returns(fileKind);

            return (codeDocument, documentSnapshot.Object);
        }
    }
}
