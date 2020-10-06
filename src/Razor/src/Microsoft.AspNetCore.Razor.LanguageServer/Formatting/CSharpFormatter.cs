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
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CSharpFormatter
    {
        private readonly RazorDocumentMappingService _documentMappingService;
        private readonly FilePathNormalizer _filePathNormalizer;
        private readonly IClientLanguageServer _server;
        private readonly object _indentationService;
        private readonly MethodInfo _getIndentationMethod;

        public CSharpFormatter(
            RazorDocumentMappingService documentMappingService,
            IClientLanguageServer languageServer,
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

            try
            {
                var type = typeof(CSharpFormattingOptions).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.Indentation.CSharpIndentationService", throwOnError: true);
                _indentationService = Activator.CreateInstance(type);
                var indentationService = type.GetInterface("IIndentationService");
                _getIndentationMethod = indentationService.GetMethod("GetIndentation");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Error occured when creating an instance of Roslyn's IIndentationService. Roslyn may have changed in an unexpected way.",
                    ex);
            }
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

        public int GetCSharpIndentation(FormattingContext context, int projectedDocumentIndex, CancellationToken cancellationToken)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Add a marker at the position where we need the indentation.
            var changedText = context.CSharpSourceText;
            var marker = $"{context.NewLineString}#line default{context.NewLineString}#line hidden{context.NewLineString}";
            changedText = changedText.WithChanges(new TextChange(TextSpan.FromBounds(projectedDocumentIndex, projectedDocumentIndex), marker));
            var changedDocument = context.CSharpWorkspaceDocument.WithText(changedText);

            // Get the line number at the position after the marker
            var line = changedText.Lines.GetLinePosition(projectedDocumentIndex + marker.Length).Line;

            try
            {
                var result = _getIndentationMethod.Invoke(
                    _indentationService,
                    new object[] { changedDocument, line, CodeAnalysis.Formatting.FormattingOptions.IndentStyle.Smart, cancellationToken });

                var baseProperty = result.GetType().GetProperty("BasePosition");
                var basePosition = (int)baseProperty.GetValue(result);
                var offsetProperty = result.GetType().GetProperty("Offset");

                // IIndentationService always returns offset as the number of spaces.
                var offset = (int)offsetProperty.GetValue(result);

                var resultLine = changedText.Lines.GetLinePosition(basePosition);
                var indentation = resultLine.Character + offset;

                return indentation;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Error occured when reflection invoking Roslyn's IIndentationService. Roslyn may have changed in an unexpected way.",
                    ex);
            }
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

            var response = _server.SendRequest(LanguageServerConstants.RazorRangeFormattingEndpoint, @params);
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
