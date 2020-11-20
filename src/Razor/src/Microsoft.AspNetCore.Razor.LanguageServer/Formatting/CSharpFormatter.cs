// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CSharpFormatter
    {
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly ClientNotifierServiceBase _server;

        public CSharpFormatter(
            RazorDocumentMappingService documentMappingService,
            ClientNotifierServiceBase languageServer,
            FilePathNormalizer filePathNormalizer)
        {
            if (documentMappingService is null)
            {
                throw new ArgumentNullException(nameof(documentMappingService));
            }

            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _documentMappingService = documentMappingService;
            _server = languageServer;
            _filePathNormalizer = filePathNormalizer;
        }

        public async Task<TextEdit[]> FormatAsync(
            FormattingContext context,
            Range rangeToFormat,
            CancellationToken cancellationToken,
            bool formatOnClient = false)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (rangeToFormat is null)
            {
                throw new ArgumentNullException(nameof(rangeToFormat));
            }

            Range projectedRange = null;
            if (rangeToFormat != null && !_documentMappingService.TryMapToProjectedDocumentRange(context.CodeDocument, rangeToFormat, out projectedRange))
            {
                return Array.Empty<TextEdit>();
            }

            TextEdit[] edits;
            if (formatOnClient)
            {
                edits = await FormatOnClientAsync(context, projectedRange, cancellationToken);
            }
            else
            {
                edits = await FormatOnServerAsync(context, projectedRange, cancellationToken);
            }

            var mappedEdits = MapEditsToHostDocument(context.CodeDocument, edits);
            return mappedEdits;
        }

        public async Task<int> GetCSharpIndentationAsync(FormattingContext context, int projectedDocumentIndex, CancellationToken cancellationToken)
        {
            var changedText = context.CSharpSourceText;
            var marker = "/*__marker__*/";
            var markerString = $"{context.NewLineString}{marker}{context.NewLineString}";
            changedText = changedText.WithChanges(new TextChange(new TextSpan(projectedDocumentIndex, 0), markerString));
            var changedDocument = context.CSharpWorkspaceDocument.WithText(changedText);

            var formattedDocument = await CodeAnalysis.Formatting.Formatter.FormatAsync(changedDocument, cancellationToken: cancellationToken);
            var formattedText = await formattedDocument.GetTextAsync(cancellationToken);
            var text = formattedText.ToString();
            var absIndex = text.IndexOf(marker, StringComparison.Ordinal);

            // Get the line number at the position after the marker
            var line = formattedText.Lines.GetLinePosition(absIndex).Line;
            if (!char.IsWhiteSpace(context.CSharpSourceText[projectedDocumentIndex]))
            {
                // If we're asked for indentation at a non-whitespace location, we want the indentation
                // of the next line after the marker.
                line += 1;
            }

            var offset = formattedText.Lines[line].GetFirstNonWhitespaceOffset() ?? 0;
            if (!context.Options.InsertSpaces)
            {
                offset *= (int)context.Options.TabSize;
            }

            return offset;
        }

        private TextEdit[] MapEditsToHostDocument(RazorCodeDocument codeDocument, TextEdit[] csharpEdits)
        {
            var actualEdits = new List<TextEdit>();
            foreach (var edit in csharpEdits)
            {
                if (_documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, edit.Range, out var newRange))
                {
                    actualEdits.Add(new TextEdit()
                    {
                        NewText = edit.NewText,
                        Range = newRange,
                    });
                }
            }

            return actualEdits.ToArray();
        }

        private async Task<TextEdit[]> FormatOnClientAsync(
            FormattingContext context,
            Range projectedRange,
            CancellationToken cancellationToken)
        {
            var @params = new RazorDocumentRangeFormattingParams()
            {
                Kind = RazorLanguageKind.CSharp,
                ProjectedRange = projectedRange,
                HostDocumentFilePath = _filePathNormalizer.Normalize(context.Uri.GetAbsoluteOrUNCPath()),
                Options = context.Options
            };

            var response = await _server.SendRequestAsync(LanguageServerConstants.RazorRangeFormattingEndpoint, @params);
            var result = await response.Returning<RazorDocumentRangeFormattingResponse>(cancellationToken);

            return result.Edits;
        }

        private async Task<TextEdit[]> FormatOnServerAsync(
            FormattingContext context,
            Range projectedRange,
            CancellationToken cancellationToken)
        {
            var csharpSourceText = context.CodeDocument.GetCSharpSourceText();
            var spanToFormat = projectedRange.AsTextSpan(csharpSourceText);
            var root = await context.CSharpWorkspaceDocument.GetSyntaxRootAsync(cancellationToken);
            var workspace = context.CSharpWorkspace;

            // Formatting options will already be set in the workspace.
            var changes = CodeAnalysis.Formatting.Formatter.GetFormattedTextChanges(root, spanToFormat, workspace);

            var edits = changes.Select(c => c.AsTextEdit(csharpSourceText)).ToArray();
            return edits;
        }
    }
}
