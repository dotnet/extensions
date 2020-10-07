// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class TypeAccessibilityCodeActionProvider : CSharpCodeActionProvider
    {
        private static readonly IEnumerable<string> SupportedDiagnostics = new[]
        {
            // `The type or namespace name 'type/namespace' could not be found
            //  (are you missing a using directive or an assembly reference?)`
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0246
            "CS0246",

            // `The name 'identifier' does not exist in the current context`
            // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/cs0103
            "CS0103",

            // `The name 'identifier' does not exist in the current context`
            "IDE1007"
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

            if (context.Request?.Context?.Diagnostics is null)
            {
                return EmptyResult;
            }

            if (codeActions is null || !codeActions.Any())
            {
                return EmptyResult;
            }

            var diagnostics = context.Request.Context.Diagnostics.Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Code?.IsString == true &&
                SupportedDiagnostics.Any(d => diagnostic.Code.Value.String.Equals(d, StringComparison.OrdinalIgnoreCase)));

            if (diagnostics is null || !diagnostics.Any())
            {
                return EmptyResult;
            }

            var results = new List<RazorCodeAction>();

            foreach (var diagnostic in diagnostics)
            {
                // Edge case handling for diagnostics which (momentarily) linger after
                // @code block is cleared out
                if (diagnostic.Range.End.Line > context.SourceText.Lines.Count ||
                    diagnostic.Range.End.Character > context.SourceText.Lines[diagnostic.Range.End.Line].End)
                {
                    continue;
                }

                var diagnosticSpan = diagnostic.Range.AsTextSpan(context.SourceText);

                // Based on how we compute `Range.AsTextSpan` it's possible to have a span
                // which goes beyond the end of the source text. Something likely changed
                // between the capturing of the diagnostic (by the platform) and the retrieval of the 
                // document snapshot / source text. In such a case, we skip processing of the diagnostic.
                if (diagnosticSpan.End > context.SourceText.Length)
                {
                    continue;
                }

                var associatedValue = context.SourceText.GetSubTextString(diagnosticSpan);

                foreach (var codeAction in codeActions)
                {
                    if (TryProcessCodeAction(
                            context,
                            codeAction,
                            diagnostic,
                            associatedValue,
                            out var typeAccessibilityCodeActions))
                    {
                        results.AddRange(typeAccessibilityCodeActions);
                    }
                }
            }

            results.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.Ordinal));
            return Task.FromResult(results as IReadOnlyList<RazorCodeAction>);
        }

        private static bool TryProcessCodeAction(
            RazorCodeActionContext context,
            RazorCodeAction codeAction,
            Diagnostic diagnostic,
            string associatedValue,
            out ICollection<RazorCodeAction> typeAccessibilityCodeActions)
        {
            var fqn = string.Empty;

            // When there's only one FQN suggestion, code action title is of the form:
            // `System.Net.Dns`
            if (!codeAction.Title.Any(c => char.IsWhiteSpace(c)) &&
                codeAction.Title.EndsWith(associatedValue, StringComparison.OrdinalIgnoreCase))
            {
                fqn = codeAction.Title;
            }
            else
            {
                // When there are multiple FQN suggestions, the code action title is of the form:
                // `Fully qualify 'Dns' -> System.Net.Dns`
                var expectedCodeActionPrefix = $"Fully qualify '{associatedValue}' -> ";
                if (codeAction.Title.StartsWith(expectedCodeActionPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    fqn = codeAction.Title.Substring(expectedCodeActionPrefix.Length);
                }
            }

            if (string.IsNullOrEmpty(fqn))
            {
                typeAccessibilityCodeActions = default;
                return false;
            }

            typeAccessibilityCodeActions = new List<RazorCodeAction>();

            var fqnCodeAction = CreateFQNCodeAction(context, diagnostic, codeAction, fqn);
            typeAccessibilityCodeActions.Add(fqnCodeAction);

            var addUsingCodeAction = CreateAddUsingCodeAction(context, fqn);
            if (addUsingCodeAction != null)
            {
                typeAccessibilityCodeActions.Add(addUsingCodeAction);
            }

            return true;
        }

        private static RazorCodeAction CreateFQNCodeAction(
            RazorCodeActionContext context,
            Diagnostic fqnDiagnostic,
            RazorCodeAction codeAction,
            string fullyQualifiedName)
        {
            var codeDocumentIdentifier = new VersionedTextDocumentIdentifier() { Uri = context.Request.TextDocument.Uri };

            var fqnTextEdit = new TextEdit()
            {
                NewText = fullyQualifiedName,
                Range = fqnDiagnostic.Range
            };

            var fqnWorkspaceEditDocumentChange = new WorkspaceEditDocumentChange(new TextDocumentEdit()
            {
                TextDocument = codeDocumentIdentifier,
                Edits = new[] { fqnTextEdit },
            });

            var fqnWorkspaceEdit = new WorkspaceEdit()
            {
                DocumentChanges = new[] { fqnWorkspaceEditDocumentChange }
            };

            return new RazorCodeAction()
            {
                Title = codeAction.Title,
                Edit = fqnWorkspaceEdit
            };
        }

        private static RazorCodeAction CreateAddUsingCodeAction(
            RazorCodeActionContext context,
            string fullyQualifiedName)
        {
            return AddUsingsCodeActionProviderFactory.CreateAddUsingCodeAction(
                fullyQualifiedName,
                context.Request.TextDocument.Uri);
        }
    }
}
