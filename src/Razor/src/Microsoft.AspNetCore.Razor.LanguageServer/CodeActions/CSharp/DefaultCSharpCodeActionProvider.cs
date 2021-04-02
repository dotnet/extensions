// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class DefaultCSharpCodeActionProvider : CSharpCodeActionProvider
    {
        // Internal for testing
        internal static readonly HashSet<string> SupportedDefaultCodeActionNames = new HashSet<string>()
        {
            RazorPredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers,
            RazorPredefinedCodeRefactoringProviderNames.AddAwait,
            RazorPredefinedCodeRefactoringProviderNames.AddDebuggerDisplay,
            RazorPredefinedCodeRefactoringProviderNames.InitializeMemberFromParameter, // Create and assign (property|field)
            RazorPredefinedCodeRefactoringProviderNames.AddParameterCheck, // Add Null checks
            RazorPredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers,
            RazorPredefinedCodeRefactoringProviderNames.GenerateDefaultConstructors,
            RazorPredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
            RazorPredefinedCodeRefactoringProviderNames.UseExpressionBody,
            RazorPredefinedCodeFixProviderNames.ImplementAbstractClass,
            RazorPredefinedCodeFixProviderNames.ImplementInterface,
            RazorPredefinedCodeFixProviderNames.SpellCheck,
            RazorPredefinedCodeFixProviderNames.RemoveUnusedVariable,
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

            // Disable multi-line code actions in @functions block
            // Will be removed once https://github.com/dotnet/aspnetcore/issues/26501 is unblocked.
            if (InFunctionsBlock(context))
            {
                return EmptyResult;
            }

            var results = new List<RazorCodeAction>();

            foreach (var codeAction in codeActions)
            {
                if (SupportedDefaultCodeActionNames.Contains(codeAction.Name))
                {
                    results.Add(codeAction.WrapResolvableCSharpCodeAction(context));
                }
            }

            return Task.FromResult(results as IReadOnlyList<RazorCodeAction>);
        }
    }
}
