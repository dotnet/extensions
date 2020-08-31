// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal abstract class FormattingPassBase : IFormattingPass
    {
        protected static readonly int DefaultOrder = 1000;

        private readonly RazorDocumentMappingService _documentMappingService;

        public FormattingPassBase(
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            IClientLanguageServer server)
        {
            if (documentMappingService is null)
            {
                throw new ArgumentNullException(nameof(documentMappingService));
            }

            if (filePathNormalizer is null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            _documentMappingService = documentMappingService;
            CSharpFormatter = new CSharpFormatter(documentMappingService, server, filePathNormalizer);
            HtmlFormatter = new HtmlFormatter(server, filePathNormalizer);
        }

        public virtual int Order => DefaultOrder;

        protected CSharpFormatter CSharpFormatter { get; }

        protected HtmlFormatter HtmlFormatter { get; }

        public virtual Task<FormattingResult> ExecuteAsync(FormattingContext context, FormattingResult result, CancellationToken cancellationToken)
        {
            return Task.FromResult(Execute(context, result));
        }

        public virtual FormattingResult Execute(FormattingContext context, FormattingResult result)
        {
            return result;
        }

        protected TextEdit[] RemapTextEdits(RazorCodeDocument codeDocument, TextEdit[] projectedTextEdits, RazorLanguageKind projectedKind)
        {
            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (projectedTextEdits is null)
            {
                throw new ArgumentNullException(nameof(projectedTextEdits));
            }

            if (projectedKind != RazorLanguageKind.CSharp)
            {
                // Non C# projections map directly to Razor. No need to remap.
                return projectedTextEdits;
            }

            var edits = new List<TextEdit>();
            for (var i = 0; i < projectedTextEdits.Length; i++)
            {
                var projectedRange = projectedTextEdits[i].Range;
                if (codeDocument.IsUnsupported() ||
                    !_documentMappingService.TryMapFromProjectedDocumentRange(codeDocument, projectedRange, out var originalRange))
                {
                    // Can't map range. Discard this edit.
                    continue;
                }

                var edit = new TextEdit()
                {
                    Range = originalRange,
                    NewText = projectedTextEdits[i].NewText
                };

                edits.Add(edit);
            }

            return edits.ToArray();
        }

        protected static TextEdit[] NormalizeTextEdits(SourceText originalText, TextEdit[] edits)
        {
            if (originalText is null)
            {
                throw new ArgumentNullException(nameof(originalText));
            }

            if (edits is null)
            {
                throw new ArgumentNullException(nameof(edits));
            }

            var changes = edits.Select(e => e.AsTextChange(originalText));
            var changedText = originalText.WithChanges(changes);
            var cleanChanges = SourceTextDiffer.GetMinimalTextChanges(originalText, changedText, lineDiffOnly: false);
            var cleanEdits = cleanChanges.Select(c => c.AsTextEdit(originalText)).ToArray();
            return cleanEdits;
        }

        // Returns the minimal TextSpan that encompasses all the differences between the old and the new text.
        protected static void TrackEncompassingChange(SourceText oldText, SourceText newText, out TextSpan spanBeforeChange, out TextSpan spanAfterChange)
        {
            if (oldText is null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            var affectedRange = newText.GetEncompassingTextChangeRange(oldText);

            spanBeforeChange = affectedRange.Span;
            spanAfterChange = new TextSpan(spanBeforeChange.Start, affectedRange.NewLength);
        }

        // This method handles adjusting of indentation of Razor blocks after C# formatter has finished formatting the document.
        // For instance, lines inside @code/@functions block should be reduced one level
        // and lines inside @{} should be reduced by two levels.
        protected static List<TextChange> AdjustCSharpIndentation(FormattingContext context, int startLine, int endLine)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var sourceText = context.SourceText;
            var editsToApply = new List<TextChange>();

            for (var i = startLine; i <= endLine; i++)
            {
                var line = sourceText.Lines[i];
                if (line.Span.Length == 0)
                {
                    // Empty line. C# formatter didn't remove it so we won't either.
                    continue;
                }

                var leadingWhitespace = line.GetLeadingWhitespace();
                var minCSharpIndentLevel = context.Indentations[i].MinCSharpIndentLevel;
                var minCSharpIndentLength = context.GetIndentationLevelString(minCSharpIndentLevel).Length;
                var desiredIndentationLevel = context.Indentations[i].IndentationLevel;
                if (leadingWhitespace.Length < minCSharpIndentLength)
                {
                    // For whatever reason, the C# formatter decided to not indent this. Leave it as is.
                    continue;
                }
                else
                {
                    // At this point we assume the C# formatter has relatively indented this line to the correct level.
                    // All we want to do at this point is to indent/unindent this line based on the absolute indentation of the block
                    // and the minimum C# indent level. We don't need to worry about the actual existing indentation here because it doesn't matter.
                    var effectiveDesiredIndentationLevel = desiredIndentationLevel - minCSharpIndentLevel;
                    var effectiveDesiredIndentation = context.GetIndentationLevelString(Math.Abs(effectiveDesiredIndentationLevel));
                    if (effectiveDesiredIndentationLevel < 0)
                    {
                        // This means that we need to unindent.
                        var span = new TextSpan(line.Start, effectiveDesiredIndentation.Length);
                        editsToApply.Add(new TextChange(span, string.Empty));
                    }
                    else if (effectiveDesiredIndentationLevel > 0)
                    {
                        // This means that we need to indent.
                        var span = new TextSpan(line.Start, 0);
                        editsToApply.Add(new TextChange(span, effectiveDesiredIndentation));
                    }
                }

            }

            return editsToApply;
        }
    }
}
