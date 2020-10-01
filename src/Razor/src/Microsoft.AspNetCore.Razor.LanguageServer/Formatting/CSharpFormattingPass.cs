// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class CSharpFormattingPass : FormattingPassBase
    {
        private readonly ILogger _logger;

        public CSharpFormattingPass(
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            IClientLanguageServer server,
            ILoggerFactory loggerFactory)
            : base(documentMappingService, filePathNormalizer, server)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<CSharpFormattingPass>();
        }

        // Run after the HTML formatter pass.
        public override int Order => DefaultOrder - 5;

        public override bool IsValidationPass => false;

        public async override Task<FormattingResult> ExecuteAsync(FormattingContext context, FormattingResult result, CancellationToken cancellationToken)
        {
            if (context.IsFormatOnType || result.Kind != RazorLanguageKind.Razor)
            {
                // We don't want to handle OnTypeFormatting here.
                return result;
            }

            // Apply previous edits if any.
            var originalText = context.SourceText;
            var changedText = originalText;
            var changedContext = context;
            if (result.Edits.Length > 0)
            {
                var changes = result.Edits.Select(e => e.AsTextChange(originalText)).ToArray();
                changedText = changedText.WithChanges(changes);
                changedContext = await context.WithTextAsync(changedText);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Apply original C# edits
            var csharpEdits = await FormatCSharpAsync(changedContext, cancellationToken);
            if (csharpEdits.Count > 0)
            {
                var csharpChanges = csharpEdits.Select(c => c.AsTextChange(changedText));
                changedText = changedText.WithChanges(csharpChanges);
                changedContext = await changedContext.WithTextAsync(changedText);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // We make an optimistic attempt at fixing corner cases.
            changedText = CleanupDocument(changedContext);
            changedContext = await changedContext.WithTextAsync(changedText);

            var indentationChanges = AdjustIndentation(changedContext, cancellationToken);
            if (indentationChanges.Count > 0)
            {
                // Apply the edits that modify indentation.
                changedText = changedText.WithChanges(indentationChanges);
            }

            var finalChanges = SourceTextDiffer.GetMinimalTextChanges(originalText, changedText, lineDiffOnly: false);
            var finalEdits = finalChanges.Select(f => f.AsTextEdit(originalText)).ToArray();

            return new FormattingResult(finalEdits);
        }

        private async Task<List<TextEdit>> FormatCSharpAsync(FormattingContext context, CancellationToken cancellationToken)
        {
            var sourceText = context.SourceText;
            var csharpEdits = new List<TextEdit>();
            foreach (var mapping in context.CodeDocument.GetCSharpDocument().SourceMappings)
            {
                var span = new TextSpan(mapping.OriginalSpan.AbsoluteIndex, mapping.OriginalSpan.Length);
                var range = span.AsRange(sourceText);
                if (!ShouldFormat(context, range.Start))
                {
                    // We don't want to format this range.
                    continue;
                }

                // These should already be remapped.
                var edits = await CSharpFormatter.FormatAsync(context, range, cancellationToken);
                csharpEdits.AddRange(edits.Where(e => range.Contains(e.Range)));
            }

            return csharpEdits;
        }

        private List<TextChange> AdjustIndentation(FormattingContext context, CancellationToken cancellationToken, Range range = null)
        {
            // In this method, the goal is to make final adjustments to the indentation of each line.
            // We will take into account the following,
            // 1. The indentation due to nested C# structures
            // 2. The indentation due to Razor and HTML constructs

            var text = context.SourceText;
            range ??= TextSpan.FromBounds(0, text.Length).AsRange(text);

            // First, let's build an understanding of the desired C# indentation at the beginning and end of each source mapping.
            var sourceMappingIndentations = new SortedDictionary<int, int>();
            foreach (var mapping in context.CodeDocument.GetCSharpDocument().SourceMappings)
            {
                var mappingSpan = new TextSpan(mapping.OriginalSpan.AbsoluteIndex, mapping.OriginalSpan.Length);
                var mappingRange = mappingSpan.AsRange(context.SourceText);
                if (!ShouldFormat(context, mappingRange.Start))
                {
                    // We don't care about this range as this can potentially lead to incorrect scopes.
                    continue;
                }

                var startIndentation = CSharpFormatter.GetCSharpIndentation(context, mapping.GeneratedSpan.AbsoluteIndex, cancellationToken);
                sourceMappingIndentations[mapping.OriginalSpan.AbsoluteIndex] = startIndentation;

                var endIndentation = CSharpFormatter.GetCSharpIndentation(context, mapping.GeneratedSpan.AbsoluteIndex + mapping.GeneratedSpan.Length + 1, cancellationToken);
                sourceMappingIndentations[mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length + 1] = endIndentation;
            }

            var sourceMappingIndentationScopes = sourceMappingIndentations.Keys.ToArray();

            // Now, let's combine the C# desired indentation with the Razor and HTML indentation for each line.
            var newIndentations = new Dictionary<int, int>();
            for (var i = range.Start.Line; i <= range.End.Line; i++)
            {
                var line = context.SourceText.Lines[i];
                if (line.Span.Length == 0)
                {
                    // We don't want to indent empty lines.
                    continue;
                }

                var lineStart = line.Start;
                int csharpDesiredIndentation;
                if (DocumentMappingService.TryMapToProjectedDocumentPosition(context.CodeDocument, lineStart, out _, out var projectedLineStart))
                {
                    // We were able to map this line to C# directly.
                    csharpDesiredIndentation = CSharpFormatter.GetCSharpIndentation(context, projectedLineStart, cancellationToken);
                }
                else
                {
                    // Couldn't remap. This is probably a non-C# location.
                    // Use SourceMapping indentations to locate the C# scope of this line.
                    // E.g,
                    //
                    // @if (true) {
                    //   <div>
                    //  |</div>
                    // }
                    //
                    // We can't find a direct mapping at |, but we can infer its base indentation from the
                    // indentation of the latest source mapping prior to this line.
                    // We use binary search to find that spot.

                    var index = Array.BinarySearch(sourceMappingIndentationScopes, lineStart);
                    if (index < 0)
                    {
                        // Couldn't find the exact value. Find the index of the element to the left of the searched value.
                        index = (~index) - 1;
                    }

                    // This will now be set to the same value as the end of the closest source mapping.
                    csharpDesiredIndentation = index < 0 ? 0 : sourceMappingIndentations[sourceMappingIndentationScopes[index]];
                }

                // Now let's use that information to figure out the effective C# indentation.
                // This should be based on context.
                // For instance, lines inside @code/@functions block should be reduced one level
                // and lines inside @{} should be reduced by two levels.

                var csharpDesiredIndentLevel = context.GetIndentationLevelForOffset(csharpDesiredIndentation);
                var minCSharpIndentLevel = context.Indentations[i].MinCSharpIndentLevel;
                if (csharpDesiredIndentLevel < minCSharpIndentLevel)
                {
                    // CSharp formatter doesn't want to indent this. Let's not touch it.
                    continue;
                }

                var effectiveCSharpDesiredIndentationLevel = csharpDesiredIndentLevel - minCSharpIndentLevel;
                var razorDesiredIndentationLevel = context.Indentations[i].IndentationLevel;
                if (!context.Indentations[i].StartsInCSharpContext)
                {
                    // This is a non-C# line. Given that the HTML formatter ran before this, we can assume
                    // HTML is already correctly formatted. So we can use the existing indentation as is.
                    razorDesiredIndentationLevel = context.GetIndentationLevelForOffset(context.Indentations[i].ExistingIndentation);
                }
                var effectiveDesiredIndentationLevel = razorDesiredIndentationLevel + effectiveCSharpDesiredIndentationLevel;

                // This will now contain the indentation we ultimately want to apply to this line.
                newIndentations[i] = effectiveDesiredIndentationLevel;
            }

            // Now that we have collected all the indentations for each line, let's convert them to text edits.
            var changes = new List<TextChange>();
            foreach (var item in newIndentations)
            {
                var line = item.Key;
                var indentationLevel = item.Value;
                Debug.Assert(indentationLevel >= 0, "Negative indent level. This is unexpected.");

                var existingIndentationLength = context.Indentations[line].ExistingIndentation;
                var spanToReplace = new TextSpan(context.SourceText.Lines[line].Start, existingIndentationLength);
                var effectiveDesiredIndentation = context.GetIndentationLevelString(indentationLevel);
                changes.Add(new TextChange(spanToReplace, effectiveDesiredIndentation));
            }

            return changes;
        }

        private static bool ShouldFormat(FormattingContext context, Position position)
        {
            // We should be called with start positions of various C# SourceMappings.
            if (position.Character == 0)
            {
                // The mapping starts at 0. It can't be anything special but pure C#. Let's format it.
                return true;
            }

            var sourceText = context.SourceText;
            var absoluteIndex = sourceText.Lines[(int)position.Line].Start + (int)position.Character;
            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            var change = new SourceChange(absoluteIndex, 0, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner == null)
            {
                // Can't determine owner of this position. Optimistically allow formatting.
                return true;
            }

            if (IsInHtmlTag() ||
                IsInSingleLineDirective() ||
                IsImplicitOrExplicitExpression())
            {
                return false;
            }

            return true;

            bool IsInHtmlTag()
            {
                // E.g, (| is position)
                //
                // `<p csharpattr="|Variable">` - true
                //
                return owner.AncestorsAndSelf().Any(
                    n => n is MarkupStartTagSyntax || n is MarkupTagHelperStartTagSyntax || n is MarkupEndTagSyntax || n is MarkupTagHelperEndTagSyntax);
            }

            bool IsInSingleLineDirective()
            {
                // E.g, (| is position)
                //
                // `@inject |SomeType SomeName` - true
                //
                // Note: @using directives don't have a descriptor associated with them, hence the extra null check.
                //
                return owner.AncestorsAndSelf().Any(
                    n => n is RazorDirectiveSyntax directive && (directive.DirectiveDescriptor == null || directive.DirectiveDescriptor.Kind == DirectiveKind.SingleLine));
            }

            bool IsImplicitOrExplicitExpression()
            {
                // E.g, (| is position)
                //
                // `@|foo` - true
                // `@(|foo)` - true
                //
                return owner.AncestorsAndSelf().Any(n => n is CSharpImplicitExpressionSyntax || n is CSharpExplicitExpressionSyntax);
            }
        }
    }
}
