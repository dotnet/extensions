// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class ImplementInterfaceAbstractClassCodeActionProvider : CSharpCodeActionProvider
    {
        // `A class is required to implement all the abstract members
        // in the base class, unless the class is also abstract.`
        // https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0534
        private static readonly string ImplementAbstractClassDiagnostic = "CS0534";
        private static readonly string ImplementAbstractClassCodeActionTitle = "Implement abstract class";


        // `'class' does not implement interface member 'member'`
        // https://docs.microsoft.com/en-us/dotnet/csharp/misc/cs0535
        private static readonly string ImplementInterfaceDiagnostic = "CS0535";
        private static readonly IEnumerable<string> ImplementInterfaceCodeActionTitle = new HashSet<string>()
        {
            "Implement interface",
            "Implement interface with Dispose pattern"
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

            if (context.Request?.Context?.Diagnostics is null)
            {
                return EmptyResult;
            }

            var diagnostics = context.Request.Context.Diagnostics.Where(diagnostic =>
                    diagnostic.Severity == DiagnosticSeverity.Error &&
                    diagnostic.Code?.IsString == true)
                .Select(diagnostic => diagnostic.Code.Value.String)
                .ToImmutableHashSet();

            if (diagnostics is null)
            {
                return null;
            }

            var results = new List<RazorCodeAction>();

            if (diagnostics.Contains(ImplementAbstractClassDiagnostic))
            {
                var implementAbstractClassCodeAction = codeActions.Where(c =>
                    c.Title == ImplementAbstractClassCodeActionTitle);
                results.AddRange(implementAbstractClassCodeAction);
            }

            if (diagnostics.Contains(ImplementInterfaceDiagnostic))
            {
                var implementInterfaceCodeActions = codeActions.Where(c =>
                    ImplementInterfaceCodeActionTitle.Contains(c.Title));
                results.AddRange(implementInterfaceCodeActions);
            }

            var wrappedResults = results.Select(c => c.WrapResolvableCSharpCodeAction(context)).ToList();
            return Task.FromResult(wrappedResults as IReadOnlyList<RazorCodeAction>);
        }
    }
}
