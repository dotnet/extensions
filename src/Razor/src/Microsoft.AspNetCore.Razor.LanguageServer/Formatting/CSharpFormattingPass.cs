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
            ProjectSnapshotManagerAccessor projectSnapshotManagerAccessor,
            ILoggerFactory loggerFactory)
            : base(documentMappingService, filePathNormalizer, server, projectSnapshotManagerAccessor)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<CSharpFormattingPass>();
        }

        // Run after the HTML formatter pass.
        public override int Order => DefaultOrder - 5;

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

            // Now, for each affected line in the edited version of the document, remove x amount of spaces
            // at the front to account for extra indentation applied by the C# formatter.
            // This should be based on context.
            // For instance, lines inside @code/@functions block should be reduced one level
            // and lines inside @{} should be reduced by two levels.
            var indentationChanges = AdjustCSharpIndentation(changedContext, startLine: 0, endLine: changedText.Lines.Count - 1);

            if (indentationChanges.Count > 0)
            {
                // Apply the edits that modify indentation.
                changedText = changedText.WithChanges(indentationChanges);
                changedContext = await changedContext.WithTextAsync(changedText);
            }

            // We make an optimistic attempt at fixing corner cases.
            changedText = CleanupDocument(changedContext);

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
                var edits = await CSharpFormatter.FormatAsync(context.CodeDocument, range, context.Uri, context.Options, cancellationToken);
                csharpEdits.AddRange(edits.Where(e => range.Contains(e.Range)));
            }

            return csharpEdits;
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
