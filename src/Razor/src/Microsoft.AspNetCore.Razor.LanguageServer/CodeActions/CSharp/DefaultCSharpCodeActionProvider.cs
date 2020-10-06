// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class DefaultCSharpCodeActionProvider : CSharpCodeActionProvider
    {
        private static readonly HashSet<Regex> RegexMatchCodeActions = new HashSet<Regex>()
        {
            // Supports generating the empty constructor `ClassName()`, as well as constructor with args `ClassName(int)`
            new Regex(@"^Generate constructor '.+\(.*\)'$", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1)),

            new Regex("^Create and assign (property|field) '.+'$", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1))
        };

        private static readonly HashSet<string> StringMatchCodeActions = new HashSet<string>()
        {
            "Generate Equals and GetHashCode",
            "Add null check",
            "Add null checks for all parameters",
            "Add 'DebuggerDisplay' attribute"
        };

        public override Task<IReadOnlyList<RazorCodeAction>> ProvideAsync(
            RazorCodeActionContext context,
            IEnumerable<RazorCodeAction> codeActions,
            CancellationToken cancellationToken)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (codeActions is null)
            {
                throw new ArgumentNullException(nameof(codeActions));
            }

            // Used to identify if this is VSCode which doesn't support
            // code action resolve.
            if (!context.SupportsCodeActionResolve)
            {
                return EmptyResult;
            }

            var results = codeActions.Where(codeAction =>
                StringMatchCodeActions.Contains(codeAction.Title) ||
                RegexMatchCodeActions.Any(pattern => pattern.Match(codeAction.Title).Success)
            );

            var wrappedResults = results.Select(c => c.WrapResolvableCSharpCodeAction(context)).ToList();
            return Task.FromResult(wrappedResults as IReadOnlyList<RazorCodeAction>);
        }
    }
}
