// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CSharpOnTypeFormattingPass : FormattingPassBase
    {
        private readonly ILogger _logger;

        public CSharpOnTypeFormattingPass(
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            ILanguageServer server,
            ILoggerFactory loggerFactory)
            : base(documentMappingService, filePathNormalizer, server)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<CSharpOnTypeFormattingPass>();
        }

        public async override Task<FormattingResult> ExecuteAsync(FormattingContext context, FormattingResult result)
        {
            if (!context.IsFormatOnType || result.Kind != RazorLanguageKind.CSharp)
            {
                // We don't want to handle regular formatting or non-C# on type formatting here.
                return result;
            }

            // Normalize and re-map the C# edits.
            var codeDocument = context.CodeDocument;
            var csharpText = SourceText.From(codeDocument.GetCSharpDocument().GeneratedCode);
            var normalizedEdits = NormalizeTextEdits(csharpText, result.Edits);
            var mappedEdits = RemapTextEdits(codeDocument, normalizedEdits, result.Kind);
            var filteredEdits = FilterCSharpTextEdits(context, mappedEdits);
            if (filteredEdits.Length == 0)
            {
                // There are no CSharp edits for us to apply. No op.
                return new FormattingResult(filteredEdits);
            }

            // Find the lines that were affected by these edits.
            var originalText = codeDocument.GetSourceText();
            var changes = filteredEdits.Select(e => e.AsTextChange(originalText));
            var changedText = originalText.WithChanges(changes);
            TrackEncompassingChange(originalText, changedText, out var spanBeforeChange, out var spanAfterChange);
            var rangeBeforeEdit = spanBeforeChange.AsRange(originalText);
            var rangeAfterEdit = spanAfterChange.AsRange(changedText);

            // Create a new formatting context for the changed razor document.
            var changedContext = await context.WithTextAsync(changedText);

            // Now, for each affected line in the edited version of the document, remove x amount of spaces
            // at the front to account for extra indentation applied by the C# formatter.
            // This should be based on context.
            // For instance, lines inside @code/@functions block should be reduced one level
            // and lines inside @{} should be reduced by two levels.
            var indentationChanges = AdjustCSharpIndentation(changedContext, (int)rangeAfterEdit.Start.Line, (int)rangeAfterEdit.End.Line);

            if (indentationChanges.Count > 0)
            {
                // Apply the edits that remove indentation.
                changedText = changedText.WithChanges(indentationChanges);
                changedContext = await changedContext.WithTextAsync(changedText);
            }

            // We make an optimistic attempt at fixing corner cases.
            changedText = CleanupDocument(changedContext, rangeAfterEdit);

            // Now that we have made all the necessary changes to the document. Let's diff the original vs final version and return the diff.
            var finalChanges = SourceTextDiffer.GetMinimalTextChanges(originalText, changedText, lineDiffOnly: false);
            var finalEdits = finalChanges.Select(f => f.AsTextEdit(originalText)).ToArray();

            return new FormattingResult(finalEdits);
        }

        private TextEdit[] FilterCSharpTextEdits(FormattingContext context, TextEdit[] edits)
        {
            var filteredEdits = edits.Where(e => !AffectsWhitespaceInNonCSharpLine(e)).ToArray();
            return filteredEdits;

            bool AffectsWhitespaceInNonCSharpLine(TextEdit edit)
            {
                //
                // Example:
                //     @{
                //       var x = "asdf";
                // |  |}
                // ^  ^ - C# formatter wants to remove this whitespace because it doesn't know about the '}'.
                // But we can't let it happen.
                //
                return
                    edit.Range.Start.Character == 0 &&
                    edit.Range.Start.Line == edit.Range.End.Line &&
                    !context.Indentations[(int)edit.Range.Start.Line].StartsInCSharpContext;
            }
        }

        private SourceText CleanupDocument(FormattingContext context, Range range = null)
        {
            //
            // We look through every source mapping that intersects with the affected range and
            // adjust the indentation of the first line,
            //
            // E.g,
            //
            // @{   public int x = 0;
            // }
            //
            // becomes,
            //
            // @{
            //    public int x  = 0;
            // }
            // 
            var text = context.SourceText;
            range ??= TextSpan.FromBounds(0, text.Length).AsRange(text);
            var csharpDocument = context.CodeDocument.GetCSharpDocument();

            var changes = new List<TextChange>();
            foreach (var mapping in csharpDocument.SourceMappings)
            {
                var mappingSpan = new TextSpan(mapping.OriginalSpan.AbsoluteIndex, mapping.OriginalSpan.Length);
                var mappingRange = mappingSpan.AsRange(text);
                if (!range.LineOverlapsWith(mappingRange))
                {
                    // We don't care about this range. It didn't change.
                    continue;
                }

                var mappingStartLineIndex = (int)mappingRange.Start.Line;
                if (context.Indentations[mappingStartLineIndex].StartsInCSharpContext)
                {
                    // Doesn't need cleaning up.
                    // For corner cases like (Range marked with |...|),
                    // @{
                    //     if (true} { <div></div>| }|
                    // }
                    // We want to leave it alone because tackling it here is really complicated.
                    continue;
                }

                // @{
                //     if (true)
                //     {     
                //         <div></div>|
                // 
                //              |}
                // }
                // We want to return the length of the range marked by |...|
                //
                var whitespaceLength = text.GetFirstNonWhitespaceOffset(mappingSpan);
                if (whitespaceLength == null)
                {
                    // There was no content here. Skip.
                    continue;
                }

                var spanToReplace = new TextSpan(mappingSpan.Start, whitespaceLength.Value);
                if (!context.TryGetIndentationLevel(spanToReplace.End, out var contentIndentLevel))
                {
                    // Can't find the correct indentation for this content. Leave it alone.
                    continue;
                }

                // At this point, `contentIndentLevel` should contain the correct indentation level for `}` in the above example.
                var replacement = context.NewLineString + context.GetIndentationLevelString(contentIndentLevel);

                // After the below change the above example should look like,
                // @{
                //     if (true)
                //     {     
                //         <div></div>
                //     }
                // }
                var change = new TextChange(spanToReplace, replacement);
                changes.Add(change);
            }

            var changedText = text.WithChanges(changes);
            return changedText;
        }
    }
}
