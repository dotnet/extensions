// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class RazorFormattingPass : FormattingPassBase
    {
        private readonly ILogger _logger;

        public RazorFormattingPass(
            RazorDocumentMappingService documentMappingService,
            FilePathNormalizer filePathNormalizer,
            ClientNotifierServiceBase server,
            ILoggerFactory loggerFactory)
            : base(documentMappingService, filePathNormalizer, server)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<RazorFormattingPass>();
        }

        // Run after the C# formatter pass.
        public override int Order => DefaultOrder - 3;

        public override bool IsValidationPass => false;

        public async override Task<FormattingResult> ExecuteAsync(FormattingContext context, FormattingResult result, CancellationToken cancellationToken)
        {
            if (context.IsFormatOnType)
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

                cancellationToken.ThrowIfCancellationRequested();
            }

            // Format the razor bits of the file
            var syntaxTree = changedContext.CodeDocument.GetSyntaxTree();
            var edits = FormatRazor(changedContext, syntaxTree);

            // Compute the final combined set of edits
            var formattingChanges = edits.Select(e => e.AsTextChange(changedText));
            changedText = changedText.WithChanges(formattingChanges);
            var finalChanges = SourceTextDiffer.GetMinimalTextChanges(originalText, changedText, lineDiffOnly: false);
            var finalEdits = finalChanges.Select(f => f.AsTextEdit(originalText)).ToArray();

            return new FormattingResult(finalEdits);
        }

        private IEnumerable<TextEdit> FormatRazor(FormattingContext context, RazorSyntaxTree syntaxTree)
        {
            var edits = new List<TextEdit>();
            var source = syntaxTree.Source;

            foreach (var node in syntaxTree.Root.DescendantNodes())
            {
                // Disclaimer: CSharpCodeBlockSyntax is used a _lot_ in razor so I'm being overly careful to only try to
                // format syntax forms we care about.
                //
                // We're looking for a code block like this:
                //
                // @code {
                //    var x = 1;
                // }
                //
                // The nodes will be a grandchild of a RazorDirective (the "@code") and we expect there to be
                // at least three children, being:
                // 1. Optional whitespace
                // 2. The opening brace
                // 3. The C# code
                // 4. The closing brace
                if (node is CSharpCodeBlockSyntax code &&
                    node.Parent?.Parent is RazorDirectiveSyntax directive &&
                    !directive.ContainsDiagnostics &&
                    directive.DirectiveDescriptor?.Kind == DirectiveKind.CodeBlock)
                {
                    var children = code.Children;
                    if (TryGetLeadingWhitespace(children, out var whitespace))
                    {
                        // For whitespace we normalize it differently depending on if its multi-line or not
                        FormatWhitespaceBetweenDirectiveAndBrace(whitespace, directive, edits, source, context);
                    }
                    else if (TryGetOpenBrace(children, out var brace))
                    {
                        // If there is no whitespace at all we normalize to a single space
                        var start = brace.GetRange(source).Start;
                        var edit = new TextEdit
                        {
                            Range = new Range(start, start),
                            NewText = " "
                        };
                        edits.Add(edit);
                    }
                }
            }

            return edits;

            static bool TryGetLeadingWhitespace(SyntaxList<RazorSyntaxNode> children, out UnclassifiedTextLiteralSyntax whitespace)
            {
                // If there is whitespace between the directive and the brace, it will be in the first child
                // of the 4 total children
                whitespace = null;
                if (children.Count == 4 && children[0] is UnclassifiedTextLiteralSyntax literal)
                {
                    whitespace = literal;
                }
                return whitespace != null;
            }

            static bool TryGetOpenBrace(SyntaxList<RazorSyntaxNode> children, out SyntaxToken brace)
            {
                // If there is no whitespace between the directive and the brace then there will only be
                // three children and the brace should be the first child
                brace = null;
                if (children.Count == 3 && children[0] is RazorMetaCodeSyntax metaCode)
                {
                    brace = metaCode.MetaCode.SingleOrDefault(m => m.Kind == SyntaxKind.LeftBrace);
                }
                return brace != null;
            }
        }

        private static void FormatWhitespaceBetweenDirectiveAndBrace(UnclassifiedTextLiteralSyntax literal, RazorDirectiveSyntax directive, List<TextEdit> edits, RazorSourceDocument source, FormattingContext context)
        {
            if (literal.LiteralTokens.Any(t => t.Kind == SyntaxKind.NewLine))
            {
                // If there is a newline then we want to have just one newline after the directive
                // and indent the { to match the @
                var edit = new TextEdit
                {
                    Range = literal.GetRange(source),
                    NewText = context.NewLineString + context.GetIndentationString(directive.GetLinePositionSpan(source).Start.Character)
                };
                edits.Add(edit);
            }
            else if (literal.Width > 1 ||
                literal.LiteralTokens[0].Content.Equals("\t"))
            {
                // If there is anything other than one single space then we replace with one space between directive and brace.
                //
                // ie, "@code     {" will become "@code {"
                var edit = new TextEdit
                {
                    Range = literal.GetRange(source),
                    NewText = " "
                };
                edits.Add(edit);
            }
        }
    }
}
