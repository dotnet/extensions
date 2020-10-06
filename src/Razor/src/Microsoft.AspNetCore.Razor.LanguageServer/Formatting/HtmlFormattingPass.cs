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
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using TextSpan = Microsoft.CodeAnalysis.Text.TextSpan;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class HtmlFormattingPass : FormattingPassBase
    {
        private readonly ILogger _logger;

        public HtmlFormattingPass(
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

            _logger = loggerFactory.CreateLogger<HtmlFormattingPass>();
        }

        // We want this to run first because it uses the client HTML formatter.
        public override int Order => DefaultOrder - 5;

        public override bool IsValidationPass => false;

        public async override Task<FormattingResult> ExecuteAsync(FormattingContext context, FormattingResult result, CancellationToken cancellationToken)
        {
            if (context.IsFormatOnType)
            {
                // We don't want to handle OnTypeFormatting here.
                return result;
            }

            var originalText = context.SourceText;

            var htmlEdits = await HtmlFormatter.FormatAsync(context, context.Range, cancellationToken);
            var normalizedEdits = NormalizeTextEdits(originalText, htmlEdits);
            var mappedEdits = RemapTextEdits(context.CodeDocument, normalizedEdits, RazorLanguageKind.Html);
            var changes = mappedEdits.Select(e => e.AsTextChange(originalText));

            var changedText = originalText;
            var changedContext = context;
            if (changes.Any())
            {
                changedText = originalText.WithChanges(changes);
                // Create a new formatting context for the changed razor document.
                changedContext = await context.WithTextAsync(changedText);
            }

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

                var razorDesiredIndentationLevel = context.Indentations[i].RazorIndentationLevel;
                if (razorDesiredIndentationLevel == 0)
                {
                    // This line isn't under any Razor specific constructs. Trust the HTML formatter.
                    continue;
                }

                var htmlDesiredIndentationLevel = context.Indentations[i].HtmlIndentationLevel;
                if (htmlDesiredIndentationLevel == 0 && !IsPartOfHtmlTag(context, context.Indentations[i].FirstSpan.Span.Start))
                {
                    // This line is under some Razor specific constructs but not under any HTML tag.
                    // E.g,
                    // @{
                    //          @* comment *@ <----
                    // }
                    //
                    // In this case, the HTML formatter wouldn't touch it but we should format it correctly.
                    // So, let's use our syntax understanding to rewrite the indentation.
                    // Note: This case doesn't apply for HTML tags (HTML formatter will touch it even if it is in the root).
                    // Hence the second part of the if condition.
                    //
                    var desiredIndentationLevel = context.Indentations[i].IndentationLevel;
                    var desiredIndentationString = context.GetIndentationLevelString(desiredIndentationLevel);
                    var spanToReplace = new TextSpan(line.Start, context.Indentations[i].ExistingIndentation);
                    var change = new TextChange(spanToReplace, desiredIndentationString);
                    editsToApply.Add(change);
                }
                else
                {
                    // This line is under some Razor specific constructs and HTML tags.
                    // E.g,
                    // @{
                    //    <div class="foo"
                    //         id="oof">  <----
                    //    </div>
                    // }
                    //
                    // In this case, the HTML formatter would've formatted it correctly. Let's not use our syntax understanding.
                    // Instead, we should just add to the existing indentation.
                    //
                    var razorDesiredIndentationString = context.GetIndentationLevelString(razorDesiredIndentationLevel);
                    var existingIndentationString = context.GetIndentationString(context.Indentations[i].ExistingIndentation);
                    var desiredIndentationString = existingIndentationString + razorDesiredIndentationString;
                    var spanToReplace = new TextSpan(line.Start, context.Indentations[i].ExistingIndentation);
                    var change = new TextChange(spanToReplace, desiredIndentationString);
                    editsToApply.Add(change);
                }
            }

            return editsToApply;
        }

        private static bool IsPartOfHtmlTag(FormattingContext context, int position)
        {
            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            var change = new SourceChange(position, 0, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner == null)
            {
                // Can't determine owner of this position.
                return false;
            }

            // E.g, (| is position)
            //
            // `<p csharpattr="|Variable">` - true
            //
            return owner.AncestorsAndSelf().Any(
                n => n is MarkupStartTagSyntax || n is MarkupTagHelperStartTagSyntax || n is MarkupEndTagSyntax || n is MarkupTagHelperEndTagSyntax);
        }
    }
}
