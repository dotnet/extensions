// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class HtmlFormattingPass : FormattingPassBase
    {
        private readonly ILogger _logger;

        public HtmlFormattingPass(
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            IClientLanguageServer server,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor,
            ILoggerFactory loggerFactory)
            : base(documentMappingService, filePathNormalizer, server, projectSnapshotManagerAccessor)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<HtmlFormattingPass>();
        }

        // We want this to run first because it uses the client HTML formatter.
        public override int Order => DefaultOrder - 5;

        public async override Task<FormattingResult> ExecuteAsync(FormattingContext context, FormattingResult result, CancellationToken cancellationToken)
        {
            if (context.IsFormatOnType)
            {
                // We don't want to handle OnTypeFormatting here.
                return result;
            }

            var originalText = context.SourceText;

            var htmlEdits = await HtmlFormatter.FormatAsync(context.CodeDocument, context.Range, context.Uri, context.Options, cancellationToken);
            var normalizedEdits = NormalizeTextEdits(originalText, htmlEdits);
            var mappedEdits = RemapTextEdits(context.CodeDocument, normalizedEdits, RazorLanguageKind.Html);
            var changes = mappedEdits.Select(e => e.AsTextChange(originalText));
            if (!changes.Any())
            {
                return result;
            }

            var changedText = originalText.WithChanges(changes);

            // Create a new formatting context for the changed razor document.
            var changedContext = await context.WithTextAsync(changedText);

            var indentationChanges = AdjustRazorIndentation(changedContext);
            if (indentationChanges.Count > 0)
            {
                // Apply the edits that adjust indentation.
                changedText = changedText.WithChanges(indentationChanges);
            }

            var finalChanges = SourceTextDiffer.GetMinimalTextChanges(originalText, changedText, lineDiffOnly: false);
            var finalEdits = finalChanges.Select(f => f.AsTextEdit(originalText)).ToArray();

            return new FormattingResult(finalEdits);
        }

        private List<TextChange> AdjustRazorIndentation(FormattingContext context)
        {
            // Assume HTML formatter has already run at this point and HTML is relatively indented correctly.
            // But HTML doesn't know about Razor blocks.
            // Our goal here is to indent each line according to the surrounding Razor blocks.
            var sourceText = context.SourceText;
            var editsToApply = new List<TextChange>();

            for (var i = 0; i < sourceText.Lines.Count; i++)
            {
                var line = sourceText.Lines[i];
                if (line.Span.Length == 0)
                {
                    // Empty line.
                    continue;
                }

                if (context.Indentations[i].StartsInCSharpContext)
                {
                    continue;
                }

                var desiredIndentationLevel = context.Indentations[i].IndentationLevel;
                var desiredIndentationString = context.GetIndentationLevelString(desiredIndentationLevel);
                var spanToReplace = new TextSpan(line.Start, context.Indentations[i].ExistingIndentation);
                var change = new TextChange(spanToReplace, desiredIndentationString);
                editsToApply.Add(change);
            }

            return editsToApply;
        }
    }
}
