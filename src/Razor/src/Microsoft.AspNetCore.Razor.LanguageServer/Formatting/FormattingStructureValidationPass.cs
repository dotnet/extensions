// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    internal class FormattingStructureValidationPass : FormattingPassBase
    {
        private readonly ILogger _logger;

        public FormattingStructureValidationPass(
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

            _logger = loggerFactory.CreateLogger<CSharpOnTypeFormattingPass>();
        }

        // We want this to run at the very end.
        public override int Order => DefaultOrder + 1000;

        public override FormattingResult Execute(FormattingContext context, FormattingResult result)
        {
            Debug.Assert(result.Kind == RazorLanguageKind.Razor, "This method shouldn't be called for projected document edits.");

            if (result.Edits.Length == 0)
            {
                // No op.
                return result;
            }

            if (!context.IsFormatOnType)
            {
                // We don't care about regular formatting for now.
                return result;
            }

            if (FormatsOutsidePureCSharpDirectiveBlocks(context, result) &&
                FormatsOutsidePureCSharpStatementBlocks(context, result))
            {
                _logger.LogDebug("A formatting result was rejected because it was going to format outside of pure C# blocks.");
                return new FormattingResult(Array.Empty<TextEdit>());
            }

            return result;
        }

        private bool FormatsOutsidePureCSharpDirectiveBlocks(FormattingContext context, FormattingResult result)
        {
            var text = context.SourceText;
            var changes = result.Edits.Select(e => e.AsTextChange(text));
            var changedText = text.WithChanges(changes);
            var affectedSpan = changedText.GetEncompassingTextChangeRange(text).Span;
            var affectedRange = affectedSpan.AsRange(text);

            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            var nodes = syntaxTree.GetCodeBlockDirectives();

            var affectedCodeDirective = nodes.FirstOrDefault(n =>
            {
                var range = n.GetRange(context.CodeDocument.Source);
                return range.Contains(affectedRange);
            });

            if (affectedCodeDirective == null)
            {
                // This edit lies outside any C# directive blocks.
                return true;
            }

            if (!(affectedCodeDirective.Body is RazorDirectiveBodySyntax directiveBody))
            {
                // This can't happen realistically. Just being defensive.
                return false;
            }

            // Get the inner code block node that contains the actual code.
            var innerCodeBlockNode = directiveBody.CSharpCode.DescendantNodes().FirstOrDefault(n => n is CSharpCodeBlockSyntax);
            if (innerCodeBlockNode == null)
            {
                // Nothing to check.
                return false;
            }

            if (ContainsNonCSharpContent(innerCodeBlockNode))
            {
                // We currently don't support formatting code block directives with Markup or other Razor constructs.

                _logger.LogDebug("A formatting result was rejected because it was going to format code directive with mixed content.");

                return true;
            }

            return false;
        }

        private bool FormatsOutsidePureCSharpStatementBlocks(FormattingContext context, FormattingResult result)
        {
            var text = context.SourceText;
            var changes = result.Edits.Select(e => e.AsTextChange(text));
            var changedText = text.WithChanges(changes);
            var affectedSpan = changedText.GetEncompassingTextChangeRange(text).Span;
            var affectedRange = affectedSpan.AsRange(text);

            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            var nodes = syntaxTree.GetCSharpStatements();

            var affectedCSharpStatement = nodes.FirstOrDefault(n =>
            {
                var range = n.GetRange(context.CodeDocument.Source);
                return range.Contains(affectedRange);
            });

            if (affectedCSharpStatement == null)
            {
                // This edit lies outside any C# statement blocks.
                return true;
            }

            if (!(affectedCSharpStatement.Body is CSharpStatementBodySyntax statementBody))
            {
                // This can't happen realistically. Just being defensive.
                return false;
            }

            // Get the inner code block node that contains the actual code.
            var innerCodeBlockNode = statementBody.CSharpCode;
            if (innerCodeBlockNode == null)
            {
                // Nothing to check.
                return false;
            }

            if (ContainsNonCSharpContent(innerCodeBlockNode))
            {
                // We currently don't support formatting statement blocks with Markup or other Razor constructs.

                _logger.LogDebug("A formatting result was rejected because it was going to format a statement block with mixed content.");

                return true;
            }

            return false;
        }

        private static bool ContainsNonCSharpContent(SyntaxNode node)
        {
            return node.DescendantNodes().Any(n =>
                n is MarkupBlockSyntax ||
                n is CSharpTransitionSyntax ||
                n is RazorCommentBlockSyntax);
        }
    }
}
