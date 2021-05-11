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
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal abstract class FormattingPassBase : IFormattingPass
    {
        protected static readonly int DefaultOrder = 1000;

        public FormattingPassBase(
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            ClientNotifierServiceBase server)
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

            DocumentMappingService = documentMappingService;
        }

        public abstract bool IsValidationPass { get; }

        public virtual int Order => DefaultOrder;

        protected RazorDocumentMappingService DocumentMappingService { get; }

        public abstract Task<FormattingResult> ExecuteAsync(FormattingContext context, FormattingResult result, CancellationToken cancellationToken);

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
                    !DocumentMappingService.TryMapFromProjectedDocumentRange(codeDocument, projectedRange, out var originalRange))
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
        protected static void TrackEncompassingChange(SourceText oldText, IEnumerable<TextChange> changes, out TextSpan spanBeforeChange, out TextSpan spanAfterChange)
        {
            if (oldText is null)
            {
                throw new ArgumentNullException(nameof(oldText));
            }

            if (changes is null)
            {
                throw new ArgumentNullException(nameof(changes));
            }

            var newText = oldText.WithChanges(changes);
            var affectedRange = newText.GetEncompassingTextChangeRange(oldText);

            spanBeforeChange = affectedRange.Span;
            spanAfterChange = new TextSpan(spanBeforeChange.Start, affectedRange.NewLength);
        }
    }
}
