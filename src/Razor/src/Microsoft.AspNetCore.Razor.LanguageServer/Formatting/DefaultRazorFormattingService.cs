// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class DefaultRazorFormattingService : RazorFormattingService
    {
        private readonly IReadOnlyList<RazorFormatOnTypeProvider> _formatOnTypeProviders;
        private readonly IReadOnlyList<string> _formatOnTypeTriggerCharacters;
        private readonly ILanguageServer _server;
        private readonly CSharpFormatter _csharpFormatter;
        private readonly HtmlFormatter _htmlFormatter;
        private readonly ILogger _logger;

        private static readonly TextEdit[] EmptyArray = Array.Empty<TextEdit>();

        public DefaultRazorFormattingService(
            IEnumerable<RazorFormatOnTypeProvider> formatOnTypeProviders,
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            ILanguageServer server,
            ILoggerFactory loggerFactory)
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

            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _formatOnTypeProviders = formatOnTypeProviders.ToList();
            _formatOnTypeTriggerCharacters = _formatOnTypeProviders.Select(provider => provider.TriggerCharacter).ToList();
            _server = server;
            _csharpFormatter = new CSharpFormatter(documentMappingService, server, filePathNormalizer);
            _htmlFormatter = new HtmlFormatter(server, filePathNormalizer);
            _logger = loggerFactory.CreateLogger<DefaultRazorFormattingService>();
        }

        public override IReadOnlyList<string> OnTypeTriggerHandlers => _formatOnTypeTriggerCharacters;

        public override Task<TextEdit[]> FormatOnTypeAsync(Uri uri, RazorCodeDocument codeDocument, Position position, string character, FormattingOptions options)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            if (string.IsNullOrEmpty(character))
            {
                throw new ArgumentNullException(nameof(character));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var applicableProviders = new List<RazorFormatOnTypeProvider>();
            for (var i = 0; i < _formatOnTypeProviders.Count; i++)
            {
                var formatOnTypeProvider = _formatOnTypeProviders[i];
                if (formatOnTypeProvider.TriggerCharacter == character)
                {
                    applicableProviders.Add(formatOnTypeProvider);
                }
            }

            if (applicableProviders.Count == 0)
            {
                // There's currently a bug in the LSP platform where other language clients OnTypeFormatting trigger characters influence every language clients trigger characters.
                // To combat this we need to pre-emptively return so we don't try having our providers handle characters that they can't.
                return Task.FromResult(EmptyArray);
            }

            var formattingContext = CreateFormattingContext(uri, codeDocument, new Range(position, position), options);
            for (var i = 0; i < applicableProviders.Count; i++)
            {
                if (applicableProviders[i].TryFormatOnType(position, formattingContext, out var textEdits))
                {
                    // We don't currently aggregate text edits from multiple providers because we're making the assumption that one set of text edits at a given position will probably
                    // clash with any other ones that are generated from another provider.
                    return Task.FromResult(textEdits);
                }
            }

            // No provider could handle the text edit.
            return Task.FromResult(EmptyArray);
        }

        public override async Task<TextEdit[]> FormatAsync(Uri uri, RazorCodeDocument codeDocument, Range range, FormattingOptions options)
        {
            if (uri is null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (codeDocument is null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (range is null)
            {
                throw new ArgumentNullException(nameof(range));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var formattingContext = CreateFormattingContext(uri, codeDocument, range, options);
            var edits = await FormatCodeBlockDirectivesAsync(formattingContext);
            return edits;
        }

        private async Task<TextEdit[]> FormatCodeBlockDirectivesAsync(FormattingContext context)
        {
            // A code block directive is any extensible directive that can contain C# code. Here is how we represent it,
            // E.g,
            //
            //     @code {  public class Foo { }  }
            // ^                                  ^ ----> Full code block directive range (Includes preceding whitespace)
            //     ^                              ^ ----> Directive range
            //      ^                             ^ ----> DirectiveBody range
            //            ^                      ^  ----> inner codeblock range
            //
            // In this method, we are going to do the following for each code block directive,
            // 1. Format the inner codeblock using the C# formatter
            // 2. Adjust the absolute indentation of the lines formatted by the C# formatter while maintaining the relative indentation
            // 3. Indent the start of the code block (@code {) correctly and move any succeeding code to a separate line
            // 4. Indent the end of the code block (}) correctly and move it to a separate line if necessary
            // 5. Once all the edits are applied, compute the diff for this particular code block and add it to the global list of edits
            //
            var source = context.CodeDocument.Source;
            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            var nodes = syntaxTree.GetCodeBlockDirectives();

            var allEdits = new List<TextEdit>();

            // Iterate in reverse so that the newline changes don't affect the next code block directive.
            for (var i = nodes.Length - 1; i >= 0; i--)
            {
                var directive = nodes[i];
                if (!(directive.Body is RazorDirectiveBodySyntax directiveBody))
                {
                    // This can't happen realistically. Just being defensive.
                    continue;
                }

                var directiveRange = directive.GetRange(source);
                if (!directiveRange.OverlapsWith(context.Range))
                {
                    // This block isn't in the selected range.
                    continue;
                }

                // Get the inner code block node that contains the actual code.
                var innerCodeBlockNode = directiveBody.CSharpCode.DescendantNodes().FirstOrDefault(n => n is CSharpCodeBlockSyntax);
                if (innerCodeBlockNode == null)
                {
                    // Nothing to indent.
                    continue;
                }

                if (innerCodeBlockNode.DescendantNodes().Any(n =>
                    n is MarkupBlockSyntax ||
                    n is CSharpTransitionSyntax ||
                    n is RazorCommentBlockSyntax))
                {
                    // We currently don't support formatting code block directives with Markup or other Razor constructs.
                    continue;
                }

                var originalText = context.SourceText;
                var changedText = originalText;
                var innerCodeBlockRange = innerCodeBlockNode.GetRange(source);

                // Compute the range inside the code block that overlaps with the provided input range.
                var csharprangeToFormat = innerCodeBlockRange.Overlap(context.Range);
                if (csharprangeToFormat != null)
                {
                    var codeEdits = await _csharpFormatter.FormatAsync(context.CodeDocument, csharprangeToFormat, context.Uri, context.Options);
                    changedText = ApplyCSharpEdits(context, innerCodeBlockRange, codeEdits, minCSharpIndentLevel: 2);
                }

                var edits = new List<TextEdit>();
                FormatCodeBlockStart(context, changedText, directiveBody, innerCodeBlockNode, edits);
                FormatCodeBlockEnd(context, changedText, directiveBody, innerCodeBlockNode, edits);
                changedText = ApplyChanges(changedText, edits.Select(e => e.AsTextChange(changedText)));

                // We've now applied all the edits we wanted to do. We now need to identify everything that changed in the given code block.
                // We need to include the preceding newline in our input range because we could have unindented the code block to achieve the correct indentation.
                // Without including the preceding newline, that edit would be lost.
                var fullCodeBlockDirectiveSpan = GetSpanIncludingPrecedingWhitespaceInLine(originalText, directive.Position, directive.EndPosition);
                var changes = Diff(originalText, changedText, fullCodeBlockDirectiveSpan);

                var transformedEdits = changes.Select(c => c.AsTextEdit(originalText));
                allEdits.AddRange(transformedEdits);
            }

            return allEdits.ToArray();
        }

        //
        // 'minCSharpIndentLevel' refers to the minimum level of how much the C# formatter would indent code.
        // @code/@functions blocks contain class members and so are typically indented by 2 levels.
        // @{} blocks are put inside method body which means they are typically indented by 3 levels.
        //
        private SourceText ApplyCSharpEdits(FormattingContext context, Range codeBlockRange, TextEdit[] edits, int minCSharpIndentLevel)
        {
            var originalText = context.SourceText;
            var originalCodeBlockSpan = codeBlockRange.AsTextSpan(originalText);

            // Sometimes the C# formatter edits outside the range we supply. Filter out those edits.
            var changes = edits.Select(e => e.AsTextChange(originalText)).Where(c => originalCodeBlockSpan.Contains(c.Span)).ToArray();
            if (changes.Length == 0)
            {
                return originalText;
            }

            // Apply the C# edits to the document.
            var changedText = originalText.WithChanges(changes);
            TrackChangeInSpan(originalText, originalCodeBlockSpan, changedText, out var changedCodeBlockSpan, out var changeEncompassingSpan);

            // We now have the changed document with C# edits. But it might be indented more/less than what we want depending on the context.
            // So, we want to bring each line to the right level of indentation based on where the block is in the document.
            // We also need to only do this for the lines that are part of the input range to respect range formatting.
            var desiredIndentationLevel = context.Indentations[(int)codeBlockRange.Start.Line].IndentationLevel + 1;
            var editsToApply = new List<TextChange>();
            var inputSpan = context.Range.AsTextSpan(originalText);
            TrackChangeInSpan(originalText, inputSpan, changedText, out var changedInputSpan, out _);
            var changedInputRange = changedInputSpan.AsRange(changedText);

            for (var i = (int)changedInputRange.Start.Line; i <= changedInputRange.End.Line; i++)
            {
                var line = changedText.Lines[i];
                if (line.Span.Length == 0)
                {
                    // Empty line. C# formatter didn't remove it so we won't either.
                    continue;
                }

                if (!changedCodeBlockSpan.Contains(line.Start))
                {
                    // Defensive check to make sure we're not handling lines that are not part of the current code block.
                    continue;
                }

                var leadingWhitespace = line.GetLeadingWhitespace();
                var minCSharpIndentLength = context.GetIndentationLevelString(minCSharpIndentLevel).Length;
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

            changedText = ApplyChanges(changedText, editsToApply);
            return changedText;
        }

        private void FormatCodeBlockStart(FormattingContext context, SourceText changedText, RazorDirectiveBodySyntax directiveBody, SyntaxNode innerCodeBlock, List<TextEdit> edits)
        {
            var sourceText = context.SourceText;
            var originalBodySpan = TextSpan.FromBounds(directiveBody.Position, directiveBody.EndPosition);
            var originalBodyRange = originalBodySpan.AsRange(sourceText);
            if (context.Range.Start.Line > originalBodyRange.Start.Line)
            {
                return;
            }

            // First line is within the selected range. Let's try and format the start.

            TrackChangeInSpan(sourceText, originalBodySpan, changedText, out var changedBodySpan, out _);
            var changedBodyRange = changedBodySpan.AsRange(changedText);

            // First, make sure the first line is indented correctly.
            var firstLine = changedText.Lines[(int)changedBodyRange.Start.Line];
            var desiredIndentationLevel = context.Indentations[firstLine.LineNumber].IndentationLevel;
            var desiredIndentation = context.GetIndentationLevelString(desiredIndentationLevel);
            var firstNonWhitespaceOffset = firstLine.GetFirstNonWhitespaceOffset();
            if (firstNonWhitespaceOffset.HasValue)
            {
                var edit = new TextEdit()
                {
                    Range = new Range(
                        new Position(firstLine.LineNumber, 0),
                        new Position(firstLine.LineNumber, firstNonWhitespaceOffset.Value)),
                    NewText = desiredIndentation
                };
                edits.Add(edit);
            }

            // We should also move any code that comes after '{' down to its own line.
            var originalInnerCodeBlockSpan = TextSpan.FromBounds(innerCodeBlock.Position, innerCodeBlock.EndPosition);
            TrackChangeInSpan(sourceText, originalInnerCodeBlockSpan, changedText, out var changedInnerCodeBlockSpan, out _);
            var innerCodeBlockRange = changedInnerCodeBlockSpan.AsRange(changedText);

            var innerCodeBlockLine = changedText.Lines[(int)innerCodeBlockRange.Start.Line];
            var textAfterBlockStart = innerCodeBlockLine.ToString().Substring(innerCodeBlock.Position - innerCodeBlockLine.Start);
            var isBlockStartOnSeparateLine = string.IsNullOrWhiteSpace(textAfterBlockStart);
            var innerCodeBlockIndentationLevel = desiredIndentationLevel + 1;
            var desiredInnerCodeBlockIndentation = context.GetIndentationLevelString(innerCodeBlockIndentationLevel);
            var whitespaceAfterBlockStart = textAfterBlockStart.GetLeadingWhitespace();

            if (!isBlockStartOnSeparateLine)
            {
                // If the first line contains code, add a newline at the beginning and indent it.
                var edit = new TextEdit()
                {
                    Range = new Range(
                        new Position(innerCodeBlockLine.LineNumber, innerCodeBlock.Position - innerCodeBlockLine.Start),
                        new Position(innerCodeBlockLine.LineNumber, innerCodeBlock.Position + whitespaceAfterBlockStart.Length - innerCodeBlockLine.Start)),
                    NewText = Environment.NewLine + desiredInnerCodeBlockIndentation
                };
                edits.Add(edit);
            }
            else
            {
                //
                // The code inside the code block directive is on its own line. Ideally the C# formatter would have already taken care of it.
                // Except, the first line of the code block is not indented because of how our SourceMappings work.
                // E.g,
                // @code {
                //     ...
                // }
                // Our source mapping for this code block only ranges between the { and }, exclusive.
                // If the C# formatter provides any edits that start from before the {, we won't be able to map it back and we will ignore it.
                // Unfortunately because of this, we partially lose some edits which would have indented the first line of the code block correctly.
                // So let's manually indent the first line here.
                //
                var innerCodeBlockText = changedText.GetSubTextString(changedInnerCodeBlockSpan);
                if (!string.IsNullOrWhiteSpace(innerCodeBlockText))
                {
                    var codeStart = innerCodeBlockText.GetFirstNonWhitespaceOffset() + changedInnerCodeBlockSpan.Start;
                    if (codeStart.HasValue && codeStart != changedInnerCodeBlockSpan.End)
                    {
                        // If we got here, it means this is a non-empty code block. We can safely indent the first line.
                        var codeStartLine = changedText.Lines.GetLineFromPosition(codeStart.Value);
                        var existingCodeStartIndentation = codeStartLine.GetFirstNonWhitespaceOffset() ?? 0;
                        var edit = new TextEdit()
                        {
                            Range = new Range(
                                new Position(codeStartLine.LineNumber, 0),
                                new Position(codeStartLine.LineNumber, existingCodeStartIndentation)),
                            NewText = desiredInnerCodeBlockIndentation
                        };
                        edits.Add(edit);
                    }
                }
            }
        }

        private void FormatCodeBlockEnd(FormattingContext context, SourceText changedText, RazorDirectiveBodySyntax directiveBody, SyntaxNode innerCodeBlock, List<TextEdit> edits)
        {
            var sourceText = context.SourceText;
            var originalBodySpan = TextSpan.FromBounds(directiveBody.Position, directiveBody.EndPosition);
            var originalBodyRange = originalBodySpan.AsRange(sourceText);
            if (context.Range.End.Line < originalBodyRange.End.Line)
            {
                return;
            }

            // Last line is within the selected range. Let's try and format the end.

            TrackChangeInSpan(sourceText, originalBodySpan, changedText, out var changedBodySpan, out _);
            var changedBodyRange = changedBodySpan.AsRange(changedText);

            var firstLine = changedText.Lines[(int)changedBodyRange.Start.Line];
            var desiredIndentationLevel = context.Indentations[firstLine.LineNumber].IndentationLevel;
            var desiredIndentation = context.GetIndentationLevelString(desiredIndentationLevel);

            // we want to keep the close '}' on its own line. So bring it to the next line.
            var originalInnerCodeBlockSpan = TextSpan.FromBounds(innerCodeBlock.Position, innerCodeBlock.EndPosition);
            TrackChangeInSpan(sourceText, originalInnerCodeBlockSpan, changedText, out var changedInnerCodeBlockSpan, out _);
            var closeCurlyLocation = changedInnerCodeBlockSpan.End;
            var closeCurlyLine = changedText.Lines.GetLineFromPosition(closeCurlyLocation);
            var firstNonWhitespaceOffset = closeCurlyLine.GetFirstNonWhitespaceOffset() ?? 0;
            if (closeCurlyLine.Start + firstNonWhitespaceOffset != closeCurlyLocation)
            {
                // This means the '}' is on the same line as some C# code.
                // Bring it down to the next line and apply the desired indentation.
                var edit = new TextEdit()
                {
                    Range = new Range(
                        new Position(closeCurlyLine.LineNumber, closeCurlyLocation - closeCurlyLine.Start),
                        new Position(closeCurlyLine.LineNumber, closeCurlyLocation - closeCurlyLine.Start)),
                    NewText = Environment.NewLine + desiredIndentation
                };
                edits.Add(edit);
            }
            else if (firstNonWhitespaceOffset != desiredIndentation.Length)
            {
                // This means the '}' is on its own line but is not indented correctly. Correct it.
                var edit = new TextEdit()
                {
                    Range = new Range(
                    new Position(closeCurlyLine.LineNumber, 0),
                    new Position(closeCurlyLine.LineNumber, firstNonWhitespaceOffset)),
                    NewText = desiredIndentation
                };
                edits.Add(edit);
            }
        }

        private SourceText ApplyChanges(SourceText original, IEnumerable<TextChange> changes)
        {
            var changed = original.WithChanges(changes);
            return changed;
        }

        private void TrackChangeInSpan(SourceText oldText, TextSpan originalSpan, SourceText newText, out TextSpan changedSpan, out TextSpan changeEncompassingSpan)
        {
            var affectedRange = newText.GetEncompassingTextChangeRange(oldText);

            // The span of text before the edit which is being changed
            changeEncompassingSpan = affectedRange.Span;

            if (!originalSpan.Contains(changeEncompassingSpan))
            {
                _logger.LogDebug($"The changed region {changeEncompassingSpan} was not a subset of the span {originalSpan} being tracked. This is unexpected.");
            }

            // We now know what was the range that changed and the length of that span after the change.
            // Let's now compute what the original span looks like after the change.
            // We know it still starts from the same location but could have grown or shrunk in length.
            // Compute the change in length and then update the original span.
            var changeInOriginalSpanLength = affectedRange.NewLength - changeEncompassingSpan.Length;
            changedSpan = TextSpan.FromBounds(originalSpan.Start, originalSpan.End + changeInOriginalSpanLength);
        }

        private TextChange[] Diff(SourceText oldText, SourceText newText, TextSpan? spanToDiff = default)
        {
            // Once https://github.com/dotnet/roslyn/issues/41413 is fixed,
            // the following lines can be replaced with `newText.GetTextChanges(oldText)`.

            var spanToTrack = spanToDiff ?? TextSpan.FromBounds(0, oldText.Length);
            TrackChangeInSpan(oldText, spanToTrack, newText, out var changedSpanToTrack, out _);
            var change = new TextChange(spanToTrack, newText.GetSubText(changedSpanToTrack).ToString());
            return new[] { change };
        }

        private static TextSpan GetSpanIncludingPrecedingWhitespaceInLine(SourceText sourceText, int start, int end)
        {
            var line = sourceText.Lines.GetLineFromPosition(start);
            var precedingLineText = sourceText.GetSubTextString(TextSpan.FromBounds(line.Start, start));
            var precedingWhitespaceLength = precedingLineText.GetTrailingWhitespace().Length;

            return TextSpan.FromBounds(start - precedingWhitespaceLength, end);
        }

        // Internal for testing
        internal static FormattingContext CreateFormattingContext(Uri uri, RazorCodeDocument codedocument, Range range, FormattingOptions options)
        {
            var result = new FormattingContext()
            {
                Uri = uri,
                CodeDocument = codedocument,
                Range = range,
                Options = options
            };

            var source = codedocument.Source;
            var syntaxTree = codedocument.GetSyntaxTree();
            var formattingSpans = syntaxTree.GetFormattingSpans();

            var total = 0;
            var previousIndentationLevel = 0;
            for (var i = 0; i < source.Lines.Count; i++)
            {
                // Get first non-whitespace character position
                var lineLength = source.Lines.GetLineLength(i);
                var nonWsChar = 0;
                for (var j = 0; j < lineLength; j++)
                {
                    var ch = source[total + j];
                    if (!char.IsWhiteSpace(ch) && !ParserHelpers.IsNewLine(ch))
                    {
                        nonWsChar = j;
                        break;
                    }
                }

                // position now contains the first non-whitespace character or 0. Get the corresponding FormattingSpan.
                if (TryGetFormattingSpan(total + nonWsChar, formattingSpans, out var span))
                {
                    result.Indentations[i] = new IndentationContext
                    {
                        Line = i,
                        IndentationLevel = span.IndentationLevel,
                        RelativeIndentationLevel = span.IndentationLevel - previousIndentationLevel,
                        ExistingIndentation = nonWsChar,
                        FirstSpan = span,
                    };
                    previousIndentationLevel = span.IndentationLevel;
                }
                else
                {
                    // Couldn't find a corresponding FormattingSpan.
                    result.Indentations[i] = new IndentationContext
                    {
                        Line = i,
                        IndentationLevel = -1,
                        RelativeIndentationLevel = previousIndentationLevel,
                        ExistingIndentation = nonWsChar,
                    };
                }

                total += lineLength;
            }

            return result;
        }

        private static bool TryGetFormattingSpan(int absoluteIndex, IReadOnlyList<FormattingSpan> formattingspans, out FormattingSpan result)
        {
            result = null;
            for (var i = 0; i < formattingspans.Count; i++)
            {
                var formattingspan = formattingspans[i];
                var span = formattingspan.Span;

                if (span.Start <= absoluteIndex)
                {
                    if (span.End >= absoluteIndex)
                    {
                        if (span.End == absoluteIndex && span.Length > 0)
                        {
                            // We're at an edge.
                            // Non-marker spans (spans.length == 0) do not own the edges after it
                            continue;
                        }

                        result = formattingspan;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
