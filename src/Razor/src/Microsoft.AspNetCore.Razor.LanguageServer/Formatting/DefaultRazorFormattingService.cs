// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class DefaultRazorFormattingService : RazorFormattingService
    {
        private readonly List<IFormattingPass> _formattingPasses;
        private readonly ILogger _logger;

        public DefaultRazorFormattingService(IEnumerable<IFormattingPass> formattingPasses, ILoggerFactory loggerFactory)
        {
            if (formattingPasses is null)
            {
                throw new ArgumentNullException(nameof(formattingPasses));
            }

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _formattingPasses = formattingPasses.OrderBy(f => f.Order).ToList();
            _logger = loggerFactory.CreateLogger<DefaultRazorFormattingService>();
        }

        public override async Task<TextEdit[]> FormatAsync(
            DocumentUri uri,
            DocumentSnapshot documentSnapshot,
            Range range,
            FormattingOptions options,
            CancellationToken cancellationToken)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (documentSnapshot is null)
            {
                throw new ArgumentNullException(nameof(documentSnapshot));
            }

            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            using var context = FormattingContext.Create(uri, documentSnapshot, codeDocument, options, range);

            var result = new FormattingResult(Array.Empty<TextEdit>());
            foreach (var pass in _formattingPasses)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result = await pass.ExecuteAsync(context, result, cancellationToken);
            }

            var filteredEdits = result.Edits.Where(e => range.LineOverlapsWith(e.Range)).ToArray();
            return filteredEdits;
        }

        public override async Task<TextEdit[]> ApplyFormattedEditsAsync(
            DocumentUri uri,
            DocumentSnapshot documentSnapshot,
            RazorLanguageKind kind,
            TextEdit[] formattedEdits,
            FormattingOptions options,
            CancellationToken cancellationToken,
            bool bypassValidationPasses = false,
            bool collapseEdits = false)
        {
            if (kind == RazorLanguageKind.Html)
            {
                // We don't support formatting HTML edits yet.
                return formattedEdits;
            }

            // If we only received a single edit, let's always return a single edit back.
            // Otherwise, merge only if explicitly asked.
            collapseEdits |= formattedEdits.Length == 1;

            var codeDocument = await documentSnapshot.GetGeneratedOutputAsync();
            using var context = FormattingContext.Create(uri, documentSnapshot, codeDocument, options, isFormatOnType: true);
            var result = new FormattingResult(formattedEdits, kind);

            foreach (var pass in _formattingPasses)
            {
                if (pass.IsValidationPass && bypassValidationPasses)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                result = await pass.ExecuteAsync(context, result, cancellationToken);
            }

            var edits = result.Edits;
            if (collapseEdits)
            {
                var collapsedEdit = MergeEdits(result.Edits, context.SourceText);
                edits = new[] { collapsedEdit };
            }

            return edits;
        }

        // Internal for testing
        internal static TextEdit MergeEdits(TextEdit[] edits, SourceText sourceText)
        {
            if (edits.Length == 1)
            {
                return edits[0];
            }

            var textChanges = new List<TextChange>();
            foreach (var edit in edits)
            {
                var change = new TextChange(edit.Range.AsTextSpan(sourceText), edit.NewText);
                textChanges.Add(change);
            }

            var changedText = sourceText.WithChanges(textChanges);
            var affectedRange = changedText.GetEncompassingTextChangeRange(sourceText);
            var spanBeforeChange = affectedRange.Span;
            var spanAfterChange = new TextSpan(spanBeforeChange.Start, affectedRange.NewLength);
            var newText = changedText.GetSubTextString(spanAfterChange);

            var encompassingChange = new TextChange(spanBeforeChange, newText);

            return encompassingChange.AsTextEdit(sourceText);
        }
    }
}
