// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal abstract class FormattingPassBase : IFormattingPass
    {
        protected static readonly int DefaultOrder = 1000;

        private readonly RazorDocumentMappingService _documentMappingService;

        public FormattingPassBase(
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            IClientLanguageServer server,
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor)
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

            if (projectSnapshotManagerAccessor is null)
            {
                throw new ArgumentNullException(nameof(projectSnapshotManagerAccessor));
            }

            _documentMappingService = documentMappingService;
            CSharpFormatter = new CSharpFormatter(documentMappingService, server, projectSnapshotManagerAccessor, filePathNormalizer);
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
                if (!context.Indentations[i].StartsInCSharpContext)
                {
                    // Not a CSharp line. Don't touch it.
                    continue;
                }

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

        protected static SourceText CleanupDocument(FormattingContext context, Range range = null)
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

                if (!ShouldCleanup(context, mappingRange.Start))
                {
                    // We don't want to run cleanup on this range.
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

        private static bool ShouldCleanup(FormattingContext context, Position position)
        {
            // We should be called with start positions of various C# SourceMappings.
            if (position.Character == 0)
            {
                // The mapping starts at 0. It can't be anything special but pure C#. Let's format it.
                return true;
            }

            var sourceText = context.SourceText;
            var absoluteIndex = sourceText.Lines[(int)position.Line].Start + (int)position.Character;
            if (IsImplicitStatement())
            {
                return false;
            }

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

            bool IsImplicitStatement()
            {
                // We will return true if the position points to the start of the C# portion of an implicit statement.
                // `@|for(...)` - true
                // `@|if(...)` - true
                // `@{|...` - false
                // `@code {|...` - false
                //

                var previousCharIndex = absoluteIndex - 1;
                var previousChar = sourceText[previousCharIndex];
                return previousChar == '@';
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
        }
    }
}
