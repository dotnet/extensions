// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class FormattingTestBase : LanguageServerTestBase
    {
        protected async Task RunFormattingTestAsync(string input, string expected, int tabSize = 4, bool insertSpaces = true, string fileKind = default)
        {
            // Arrange
            var start = input.IndexOf('|');
            var end = input.LastIndexOf('|');
            input = input.Replace("|", string.Empty);

            var source = SourceText.From(input);
            var span = TextSpan.FromBounds(start, end - 1);
            var range = span.AsRange(source);

            var path = "file:///path/to/document.razor";
            var uri = new Uri(path);
            var codeDocument = CreateCodeDocument(source, uri.AbsolutePath, fileKind: fileKind);
            var options = new FormattingOptions()
            {
                TabSize = tabSize,
                InsertSpaces = insertSpaces,
            };

            var formattingService = CreateFormattingService(codeDocument);

            // Act
            var edits = await formattingService.FormatAsync(uri, codeDocument, range, options);

            // Assert
            var edited = ApplyEdits(source, edits);
            var actual = edited.ToString();
            Assert.Equal(expected, actual);
        }

        private SourceText ApplyEdits(SourceText source, TextEdit[] edits)
        {
            var changes = edits.Select(e => e.AsTextChange(source));
            return source.WithChanges(changes);
        }

        private static RazorCodeDocument CreateCodeDocument(SourceText text, string path, IReadOnlyList<TagHelperDescriptor> tagHelpers = null, string fileKind = default)
        {
            fileKind ??= FileKinds.Component;
            tagHelpers ??= Array.Empty<TagHelperDescriptor>();
            var sourceDocument = text.GetRazorSourceDocument(path, path);
            var projectEngine = RazorProjectEngine.Create(builder => { });
            var codeDocument = projectEngine.ProcessDesignTime(sourceDocument, fileKind, Array.Empty<RazorSourceDocument>(), tagHelpers);
            return codeDocument;
        }

        private RazorFormattingService CreateFormattingService(RazorCodeDocument codeDocument)
        {
            var mappingService = new DefaultRazorDocumentMappingService();
            var filePathNormalizer = new FilePathNormalizer();

            var client = new FormattingLanguageServerClient();
            client.AddCodeDocument(codeDocument);
            var languageServer = Mock.Of<ILanguageServer>(ls => ls.Client == client);

            return new DefaultRazorFormattingService(mappingService, filePathNormalizer, languageServer, LoggerFactory);
        }
    }
}
