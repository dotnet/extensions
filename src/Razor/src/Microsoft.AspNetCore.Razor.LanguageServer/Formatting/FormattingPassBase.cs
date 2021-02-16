// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
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
            CSharpFormatter = new CSharpFormatter(documentMappingService, server, filePathNormalizer);
            HtmlFormatter = new HtmlFormatter(server, filePathNormalizer);
        }

        public abstract bool IsValidationPass { get; }

        public virtual int Order => DefaultOrder;

        protected RazorDocumentMappingService DocumentMappingService { get; }

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

        protected async Task<List<TextChange>> AdjustIndentationAsync(FormattingContext context, CancellationToken cancellationToken, Range range = null)
        {
            // In this method, the goal is to make final adjustments to the indentation of each line.
            // We will take into account the following,
            // 1. The indentation due to nested C# structures
            // 2. The indentation due to Razor and HTML constructs

            var text = context.SourceText;
            range ??= TextSpan.FromBounds(0, text.Length).AsRange(text);

            // To help with figuring out the correct indentation, first we will need the indentation
            // that the C# formatter wants to apply in the following locations,
            // 1. The start and end of each of our source mappings
            // 2. The start of every line that starts in C# context

            // Due to perf concerns, we only want to invoke the real C# formatter once.
            // So, let's collect all the significant locations that we want to obtain the CSharpDesiredIndentations for.

            var significantLocations = new HashSet<int>();

            // First, collect all the locations at the beginning and end of each source mapping.
            var sourceMappingMap = new Dictionary<int, int>();
            foreach (var mapping in context.CodeDocument.GetCSharpDocument().SourceMappings)
            {
                var mappingSpan = new TextSpan(mapping.OriginalSpan.AbsoluteIndex, mapping.OriginalSpan.Length);
                if (!ShouldFormat(context, mappingSpan, allowImplicitStatements: true))
                {
                    // We don't care about this range as this can potentially lead to incorrect scopes.
                    continue;
                }

                var originalStartLocation = mapping.OriginalSpan.AbsoluteIndex;
                var projectedStartLocation = mapping.GeneratedSpan.AbsoluteIndex;
                sourceMappingMap[originalStartLocation] = projectedStartLocation;
                significantLocations.Add(projectedStartLocation);

                var originalEndLocation = mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length + 1;
                var projectedEndLocation = mapping.GeneratedSpan.AbsoluteIndex + mapping.GeneratedSpan.Length + 1;
                sourceMappingMap[originalEndLocation] = projectedEndLocation;
                significantLocations.Add(projectedEndLocation);
            }

            // Next, collect all the line starts that start in C# context
            var lineStartMap = new Dictionary<int, int>();
            for (var i = range.Start.Line; i <= range.End.Line; i++)
            {
                if (context.Indentations[i].EmptyOrWhitespaceLine)
                {
                    // We should remove whitespace on empty lines.
                    continue;
                }

                var line = context.SourceText.Lines[i];
                var lineStart = line.GetFirstNonWhitespacePosition() ?? line.Start;

                var lineStartSpan = new TextSpan(lineStart, 0);
                if (!ShouldFormat(context, lineStartSpan, allowImplicitStatements: true))
                {
                    // We don't care about this range as this can potentially lead to incorrect scopes.
                    continue;
                }

                if (DocumentMappingService.TryMapToProjectedDocumentPosition(context.CodeDocument, lineStart, out _, out var projectedLineStart))
                {
                    lineStartMap[lineStart] = projectedLineStart;
                    significantLocations.Add(projectedLineStart);
                }
            }

            // Now, invoke the C# formatter to obtain the CSharpDesiredIndentation for all significant locations.
            var significantLocationIndentation = await CSharpFormatter.GetCSharpIndentationAsync(context, significantLocations, cancellationToken);

            // Build source mapping indentation scopes.
            var sourceMappingIndentations = new SortedDictionary<int, int>();
            foreach (var originalLocation in sourceMappingMap.Keys)
            {
                var significantLocation = sourceMappingMap[originalLocation];
                if (!significantLocationIndentation.TryGetValue(significantLocation, out var indentation))
                {
                    // C# formatter didn't return an indentation for this. Skip.
                    continue;
                }

                sourceMappingIndentations[originalLocation] = indentation;
            }
            var sourceMappingIndentationScopes = sourceMappingIndentations.Keys.ToArray();

            // Build lineStart indentation map.
            var lineStartIndentations = new Dictionary<int, int>();
            foreach (var originalLocation in lineStartMap.Keys)
            {
                var significantLocation = lineStartMap[originalLocation];
                if (!significantLocationIndentation.TryGetValue(significantLocation, out var indentation))
                {
                    // C# formatter didn't return an indentation for this. Skip.
                    continue;
                }

                lineStartIndentations[originalLocation] = indentation;
            }

            // Now, let's combine the C# desired indentation with the Razor and HTML indentation for each line.
            var newIndentations = new Dictionary<int, int>();
            for (var i = range.Start.Line; i <= range.End.Line; i++)
            {
                if (context.Indentations[i].EmptyOrWhitespaceLine)
                {
                    // We should remove whitespace on empty lines.
                    newIndentations[i] = 0;
                    continue;
                }

                var minCSharpIndentation = context.GetIndentationOffsetForLevel(context.Indentations[i].MinCSharpIndentLevel);
                var line = context.SourceText.Lines[i];
                var lineStart = line.GetFirstNonWhitespacePosition() ?? line.Start;
                var lineStartSpan = new TextSpan(lineStart, 0);
                if (!ShouldFormat(context, lineStartSpan, allowImplicitStatements: true))
                {
                    // We don't care about this line as it lies in an area we don't want to format.
                    continue;
                }

                if (!lineStartIndentations.TryGetValue(lineStart, out var csharpDesiredIndentation))
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
                    if (index < 0)
                    {
                        csharpDesiredIndentation = 0;
                    }
                    else
                    {
                        var absoluteIndex = sourceMappingIndentationScopes[index];
                        csharpDesiredIndentation = sourceMappingIndentations[absoluteIndex];

                        // This means we didn't find an exact match and so we used the indentation of the end of a previous mapping.
                        // So let's use the MinCSharpIndentation of that same location if possible.
                        if (context.TryGetFormattingSpan(absoluteIndex, out var span))
                        {
                            minCSharpIndentation = context.GetIndentationOffsetForLevel(span.MinCSharpIndentLevel);
                        }
                    }
                }

                // Now let's use that information to figure out the effective C# indentation.
                // This should be based on context.
                // For instance, lines inside @code/@functions block should be reduced one level
                // and lines inside @{} should be reduced by two levels.

                if (csharpDesiredIndentation < minCSharpIndentation)
                {
                    // CSharp formatter doesn't want to indent this. Let's not touch it.
                    continue;
                }

                var effectiveCSharpDesiredIndentation = csharpDesiredIndentation - minCSharpIndentation;
                var razorDesiredIndentation = context.GetIndentationOffsetForLevel(context.Indentations[i].IndentationLevel);
                if (context.Indentations[i].StartsInHtmlContext)
                {
                    // This is a non-C# line.
                    if (context.IsFormatOnType)
                    {
                        // HTML formatter doesn't run in the case of format on type.
                        // Let's stick with our syntax understanding of HTML to figure out the desired indentation.
                    }
                    else
                    {
                        // Given that the HTML formatter ran before this, we can assume
                        // HTML is already correctly formatted. So we can use the existing indentation as is.
                        razorDesiredIndentation = context.Indentations[i].ExistingIndentation;
                    }
                }
                var effectiveDesiredIndentation = razorDesiredIndentation + effectiveCSharpDesiredIndentation;

                // This will now contain the indentation we ultimately want to apply to this line.
                newIndentations[i] = effectiveDesiredIndentation;
            }

            // Now that we have collected all the indentations for each line, let's convert them to text edits.
            var changes = new List<TextChange>();
            foreach (var item in newIndentations)
            {
                var line = item.Key;
                var indentation = item.Value;
                Debug.Assert(indentation >= 0, "Negative indentation. This is unexpected.");

                var existingIndentationLength = context.Indentations[line].ExistingIndentation;
                var spanToReplace = new TextSpan(context.SourceText.Lines[line].Start, existingIndentationLength);
                var effectiveDesiredIndentation = context.GetIndentationString(indentation);
                changes.Add(new TextChange(spanToReplace, effectiveDesiredIndentation));
            }

            return changes;
        }

        protected List<TextChange> CleanupDocument(FormattingContext context, Range range = null)
        {
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

                CleanupSourceMappingStart(context, mappingRange, changes);

                CleanupSourceMappingEnd(context, mappingRange, changes);
            }

            return changes;
        }

        private void CleanupSourceMappingStart(FormattingContext context, Range sourceMappingRange, List<TextChange> changes)
        {
            //
            // We look through every source mapping that intersects with the affected range and
            // bring the first line to its own line and adjust its indentation,
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
            var sourceMappingSpan = sourceMappingRange.AsTextSpan(text);
            if (!ShouldFormat(context, sourceMappingSpan, allowImplicitStatements: false))
            {
                // We don't want to run cleanup on this range.
                return;
            }

            if (sourceMappingRange.Start.Character == 0)
            {
                // It already starts on a fresh new line which doesn't need cleanup.
                // E.g, (The mapping starts at | in the below case)
                // @{
                //     @: Some html
                // |   var x = 123;
                // }
                //

                return;
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
            var whitespaceLength = text.GetFirstNonWhitespaceOffset(sourceMappingSpan, out var newLineCount);
            if (whitespaceLength == null)
            {
                // There was no content after the start of this mapping. Meaning it already is clean.
                // E.g,
                // @{|
                //    ...
                // }

                return;
            }

            var spanToReplace = new TextSpan(sourceMappingSpan.Start, whitespaceLength.Value);
            if (!context.TryGetIndentationLevel(spanToReplace.End, out var contentIndentLevel))
            {
                // Can't find the correct indentation for this content. Leave it alone.
                return;
            }

            // At this point, `contentIndentLevel` should contain the correct indentation level for `}` in the above example.
            // Make sure to preserve the same number of blank lines as the original string had
            var replacement = PrependLines(context.GetIndentationLevelString(contentIndentLevel), context.NewLineString, Math.Max(newLineCount, 1));

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

        private static string PrependLines(string text, string newLine, int count)
        {
            var builder = new StringBuilder(newLine.Length * count + text.Length);
            for (var i = 0; i < count; i++)
            {
                builder.Append(newLine);
            }

            builder.Append(text);
            return builder.ToString();
        }

        private void CleanupSourceMappingEnd(FormattingContext context, Range sourceMappingRange, List<TextChange> changes)
        {
            //
            // We look through every source mapping that intersects with the affected range and
            // bring the content after the last line to its own line and adjust its indentation,
            //
            // E.g,
            //
            // @{
            //     if (true)
            //     {  <div></div>
            //     }
            // }
            //
            // becomes,
            //
            // @{
            //    if (true)
            //    {
            //        </div></div>
            //    }
            // }
            //

            var text = context.SourceText;
            var sourceMappingSpan = sourceMappingRange.AsTextSpan(text);
            var mappingEndLineIndex = sourceMappingRange.End.Line;
            if (!context.Indentations[mappingEndLineIndex].StartsInCSharpContext)
            {
                // For corner cases like (Position marked with |),
                // It is already in a separate line. It doesn't need cleaning up.
                // @{
                //     if (true}
                //     {
                //         |<div></div>
                //     }
                // }
                //
                return;
            }

            var endSpan = TextSpan.FromBounds(sourceMappingSpan.End, sourceMappingSpan.End);
            if (!ShouldFormat(context, endSpan, allowImplicitStatements: false))
            {
                // We don't want to run cleanup on this range.
                return;
            }

            var contentStartOffset = text.Lines[mappingEndLineIndex].GetFirstNonWhitespaceOffset(sourceMappingRange.End.Character);
            if (contentStartOffset == null)
            {
                // There is no content after the end of this source mapping. No need to clean up.
                return;
            }

            var spanToReplace = new TextSpan(sourceMappingSpan.End, 0);
            if (!context.TryGetIndentationLevel(spanToReplace.End, out var contentIndentLevel))
            {
                // Can't find the correct indentation for this content. Leave it alone.
                return;
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

        protected bool ShouldFormat(FormattingContext context, TextSpan mappingSpan, bool allowImplicitStatements)
        {
            // We should be called with the range of various C# SourceMappings.

            if (mappingSpan.Start == 0)
            {
                // The mapping starts at 0. It can't be anything special but pure C#. Let's format it.
                return true;
            }

            var sourceText = context.SourceText;
            var absoluteIndex = mappingSpan.Start;

            if (mappingSpan.Length > 0)
            {
                // Slightly ugly hack to get around the behavior of LocateOwner.
                // In some cases, using the start of a mapping doesn't work well
                // because LocateOwner returns the previous node due to it owning the edge.
                // So, if we can try to find the owner using a position that fully belongs to the current mapping.
                absoluteIndex = mappingSpan.Start + 1;
            }

            var change = new SourceChange(absoluteIndex, 0, string.Empty);
            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner == null)
            {
                // Can't determine owner of this position. Optimistically allow formatting.
                return true;
            }

            if (IsInHtmlTag() ||
                IsInSingleLineDirective() ||
                IsImplicitOrExplicitExpression() ||
                IsInSectionDirective() ||
                (!allowImplicitStatements && IsImplicitStatementStart()))
            {
                return false;
            }

            return true;

            bool IsImplicitStatementStart()
            {
                // We will return true if the position points to the start of the C# portion of an implicit statement.
                // `@|for(...)` - true
                // `@|if(...)` - true
                // `@{|...` - false
                // `@code {|...` - false
                //

                if (owner.SpanStart == mappingSpan.Start &&
                    owner is CSharpStatementLiteralSyntax &&
                    owner.Parent is CSharpCodeBlockSyntax &&
                    owner.PreviousSpan() is CSharpTransitionSyntax)
                {
                    return true;
                }

                // Not an implicit statement.
                return false;
            }

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

            bool IsInSectionDirective()
            {
                var directive = owner.FirstAncestorOrSelf<RazorDirectiveSyntax>();
                if (directive != null &&
                    directive.DirectiveDescriptor.Directive == SectionDirective.Directive.Directive)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
