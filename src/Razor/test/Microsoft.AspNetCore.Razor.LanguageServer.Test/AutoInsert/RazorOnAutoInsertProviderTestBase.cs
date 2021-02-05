// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    public abstract class RazorOnAutoInsertProviderTestBase : LanguageServerTestBase
    {
        internal abstract RazorOnAutoInsertProvider CreateProvider();

        protected void RunAutoInsertTest(string input, string expected, int tabSize = 4, bool insertSpaces = true, string fileKind = default, IReadOnlyList<TagHelperDescriptor> tagHelpers = default)
        {
            // Arrange
            TestFileMarkupParser.GetPosition(input, out input, out var location);

            var source = SourceText.From(input);
            source.GetLineAndOffset(location, out var line, out var column);
            var position = new Position(line, column);

            var path = "file:///path/to/document.razor";
            var uri = new Uri(path);
            var codeDocument = CreateCodeDocument(source, uri.AbsolutePath, tagHelpers, fileKind: fileKind);
            var options = new FormattingOptions()
            {
                TabSize = tabSize,
                InsertSpaces = insertSpaces,
            };

            var provider = CreateProvider();
            var context = FormattingContext.Create(uri, Mock.Of<DocumentSnapshot>(MockBehavior.Strict), codeDocument, options, new Range(position, position));

            // Act
            if (!provider.TryResolveInsertion(position, context, out var edit, out var format))
            {
                edit = null;
            }

            // Assert
            var edited = edit == null ? source : ApplyEdit(source, edit);
            var actual = edited.ToString();
            Assert.Equal(expected, actual);
        }

        private SourceText ApplyEdit(SourceText source, TextEdit edit)
        {
            var change = edit.AsTextChange(source);
            return source.WithChanges(change);
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
    }
}
