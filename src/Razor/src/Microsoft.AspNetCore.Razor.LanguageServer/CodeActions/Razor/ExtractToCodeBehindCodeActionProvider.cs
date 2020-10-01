// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class ExtractToCodeBehindCodeActionProvider : RazorCodeActionProvider
    {
        private static readonly string Title = "Extract block to code behind";

        private static readonly Task<IReadOnlyList<RazorCodeAction>> EmptyResult = Task.FromResult<IReadOnlyList<RazorCodeAction>>(null);

        public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
        {
            if (context is null)
            {
                return EmptyResult;
            }

            if (!context.SupportsFileCreation)
            {
                return EmptyResult;
            }

            if (!FileKinds.IsComponent(context.CodeDocument.GetFileKind()))
            {
                return EmptyResult;
            }

            var change = new SourceChange(context.Location.AbsoluteIndex, length: 0, newText: string.Empty);
            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            if (syntaxTree?.Root is null)
            {
                return EmptyResult;
            }

            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner == null)
            {
                Debug.Fail("Owner should never be null.");
                return EmptyResult;
            }

            var node = owner.Ancestors().FirstOrDefault(n => n.Kind == SyntaxKind.RazorDirective);
            if (node == null || !(node is RazorDirectiveSyntax directiveNode))
            {
                return EmptyResult;
            }

            // Make sure we've found a @code or @functions
            if (directiveNode.DirectiveDescriptor != ComponentCodeDirective.Directive &&
                directiveNode.DirectiveDescriptor != FunctionsDirective.Directive)
            {
                return EmptyResult;
            }

            // No code action if malformed
            if (directiveNode.GetDiagnostics().Any(d => d.Severity == RazorDiagnosticSeverity.Error))
            {
                return EmptyResult;
            }

            var csharpCodeBlockNode = directiveNode.Body.DescendantNodes().FirstOrDefault(n => n is CSharpCodeBlockSyntax);
            if (csharpCodeBlockNode is null)
            {
                return EmptyResult;
            }

            if (HasUnsupportedChildren(csharpCodeBlockNode))
            {
                return EmptyResult;
            }

            // Do not provide code action if the cursor is inside the code block
            if (context.Location.AbsoluteIndex > csharpCodeBlockNode.SpanStart)
            {
                return EmptyResult;
            }

            var actionParams = new ExtractToCodeBehindCodeActionParams()
            {
                Uri = context.Request.TextDocument.Uri,
                ExtractStart = csharpCodeBlockNode.Span.Start,
                ExtractEnd = csharpCodeBlockNode.Span.End,
                RemoveStart = directiveNode.Span.Start,
                RemoveEnd = directiveNode.Span.End
            };

            var resolutionParams = new RazorCodeActionResolutionParams()
            {
                Action = LanguageServerConstants.CodeActions.ExtractToCodeBehindAction,
                Language = LanguageServerConstants.CodeActions.Languages.Razor,
                Data = actionParams,
            };

            var codeAction = new RazorCodeAction()
            {
                Title = Title,
                Data = resolutionParams
            };

            var codeActions = new List<RazorCodeAction> { codeAction };

            return Task.FromResult(codeActions as IReadOnlyList<RazorCodeAction>);
        }

        private static bool HasUnsupportedChildren(Language.Syntax.SyntaxNode node)
        {
            return node.DescendantNodes().Any(n =>
                n is MarkupBlockSyntax ||
                n is CSharpTransitionSyntax ||
                n is RazorCommentBlockSyntax);
        }
    }
}
