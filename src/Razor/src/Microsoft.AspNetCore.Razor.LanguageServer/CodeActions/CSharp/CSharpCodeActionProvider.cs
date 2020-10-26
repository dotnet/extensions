// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal abstract class CSharpCodeActionProvider
    {
        protected static readonly Task<IReadOnlyList<CodeAction>> EmptyResult =
            Task.FromResult(Array.Empty<CodeAction>() as IReadOnlyList<CodeAction>);

        public abstract Task<IReadOnlyList<CodeAction>> ProvideAsync(
            RazorCodeActionContext context,
            IEnumerable<CodeAction> codeActions,
            CancellationToken cancellationToken);

        protected bool InFunctionsBlock(RazorCodeActionContext context)
        {
            var change = new SourceChange(context.Location.AbsoluteIndex, length: 0, newText: string.Empty);
            var syntaxTree = context.CodeDocument.GetSyntaxTree();
            if (syntaxTree?.Root is null)
            {
                return false;
            }

            var owner = syntaxTree.Root.LocateOwner(change);
            if (owner == null)
            {
                Debug.Fail("Owner should never be null.");
                return false;
            }

            var node = owner.Ancestors().FirstOrDefault(n => n.Kind == SyntaxKind.RazorDirective);
            if (node == null || !(node is RazorDirectiveSyntax directiveNode))
            {
                return false;
            }

            return directiveNode.DirectiveDescriptor == FunctionsDirective.Directive;
        }
    }
}
