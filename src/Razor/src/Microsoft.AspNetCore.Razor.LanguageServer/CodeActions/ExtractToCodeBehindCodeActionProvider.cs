// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class ExtractToCodeBehindCodeActionProvider : RazorCodeActionProvider
    {
        private static readonly Task<CommandOrCodeActionContainer> EmptyResult = Task.FromResult<CommandOrCodeActionContainer>(null);

        override public Task<CommandOrCodeActionContainer> ProvideAsync(RazorCodeActionContext context, CancellationToken cancellationToken)
        {
            if (context is null)
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
            var node = owner.Ancestors().FirstOrDefault(n => n.Kind == SyntaxKind.RazorDirective);
            if (node == null || !(node is RazorDirectiveSyntax directiveNode))
            {
                return EmptyResult;
            }

            // Make sure we've found a @code or @functions
            if (directiveNode.DirectiveDescriptor != ComponentCodeDirective.Directive && directiveNode.DirectiveDescriptor != FunctionsDirective.Directive)
            {
                return EmptyResult;
            }

            // No code action if malformed
            if (directiveNode.GetDiagnostics().Any(d => d.Severity == RazorDiagnosticSeverity.Error))
            {
                return EmptyResult;
            }

            var cSharpCodeBlockNode = directiveNode.Body.DescendantNodes().FirstOrDefault(n => n is CSharpCodeBlockSyntax);
            if (cSharpCodeBlockNode is null)
            {
                return EmptyResult;
            }

            if (HasUnsupportedChildren(cSharpCodeBlockNode))
            {
                return EmptyResult;
            }

            // Do not provide code action if the cursor is inside the code block
            if (context.Location.AbsoluteIndex > cSharpCodeBlockNode.SpanStart)
            {
                return EmptyResult;
            }

            var actionParams = new ExtractToCodeBehindCodeActionParams()
            {
                Uri = context.Request.TextDocument.Uri,
                ExtractStart = cSharpCodeBlockNode.Span.Start,
                ExtractEnd = cSharpCodeBlockNode.Span.End,
                RemoveStart = directiveNode.Span.Start,
                RemoveEnd = directiveNode.Span.End
            };
            var data = JObject.FromObject(actionParams);

            var resolutionParams = new RazorCodeActionResolutionParams()
            {
                Action = LanguageServerConstants.CodeActions.ExtractToCodeBehindAction,
                Data = data,
            };
            var serializedParams = JToken.FromObject(resolutionParams);
            var arguments = new JArray(serializedParams);

            var container = new List<CommandOrCodeAction>
            {
                new Command()
                {
                    Title = "Extract block to code behind",
                    Name = LanguageServerConstants.RazorCodeActionRunnerCommand,
                    Arguments = arguments,
                }
            };

            return Task.FromResult((CommandOrCodeActionContainer)container);
        }

        private static bool HasUnsupportedChildren(Language.Syntax.SyntaxNode node)
        {
            return node.DescendantNodes().Any(n => n is MarkupBlockSyntax || n is CSharpTransitionSyntax || n is RazorCommentBlockSyntax);
        }
    }
}
