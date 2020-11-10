// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
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

        public override Task<IReadOnlyList<CodeAction>> ProvideAsync(
            RazorCodeActionContext context,
            IEnumerable<CodeAction> codeActions,
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

            var results = new List<CodeAction>();

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
            return Task.FromResult(results as IReadOnlyList<CodeAction>);
        }

        private static bool TryProcessCodeAction(
            RazorCodeActionContext context,
            CodeAction codeAction,
            Diagnostic diagnostic,
            string associatedValue,
            out ICollection<CodeAction> typeAccessibilityCodeActions)
        {
            // VS & VSCode provide type accessibility code actions in different formats
            // We must handle them seperately.
            return context.SupportsCodeActionResolve ?
                TryProcessCodeActionVS(context, codeAction, diagnostic, associatedValue, out typeAccessibilityCodeActions) :
                TryProcessCodeActionVSCode(context, codeAction, diagnostic, associatedValue, out typeAccessibilityCodeActions);
        }

        private static bool TryProcessCodeActionVSCode(
            RazorCodeActionContext context,
            CodeAction codeAction,
            Diagnostic diagnostic,
            string associatedValue,
            out ICollection<CodeAction> typeAccessibilityCodeActions)
        {
            var fqn = string.Empty;

            // When there's only one FQN suggestion, code action title is of the form:
            // `System.Net.Dns`
            if (!codeAction.Title.Any(c => char.IsWhiteSpace(c)) &&
                codeAction.Title.EndsWith(associatedValue, StringComparison.OrdinalIgnoreCase))
            {
                fqn = codeAction.Title;
            }
            // When there are multiple FQN suggestions, the code action title is of the form:
            // `Fully qualify 'Dns' -> System.Net.Dns`
            else
            {
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

            typeAccessibilityCodeActions = new List<CodeAction>();

            var fqnCodeAction = CreateFQNCodeAction(context, diagnostic, codeAction, fqn);
            typeAccessibilityCodeActions.Add(fqnCodeAction);

            var addUsingCodeAction = AddUsingsCodeActionProviderFactory.CreateAddUsingCodeAction(fqn, context.Request.TextDocument.Uri);
            if (addUsingCodeAction != null)
            {
                typeAccessibilityCodeActions.Add(addUsingCodeAction);
            }

            return true;
        }

        private static bool TryProcessCodeActionVS(
            RazorCodeActionContext context,
            CodeAction codeAction,
            Diagnostic diagnostic,
            string associatedValue,
            out ICollection<CodeAction> typeAccessibilityCodeActions)
        {
            CodeAction processedCodeAction = null;

            // When there's only one FQN suggestion, code action title is of the form:
            // `System.Net.Dns`
            if (!codeAction.Title.Any(c => char.IsWhiteSpace(c)) &&
                codeAction.Title.EndsWith(associatedValue, StringComparison.OrdinalIgnoreCase))
            {
                var fqn = codeAction.Title;
                processedCodeAction = CreateFQNCodeAction(context, diagnostic, codeAction, fqn);
            }
            // When there are multiple FQN suggestions, the code action title is of the form:
            // `Fully qualify 'Dns'`
            else if (codeAction.Title.Equals($"Fully qualify '{associatedValue}'", StringComparison.OrdinalIgnoreCase))
            {
                // Not currently supported as we need O# CodeAction to support the CodeAction.Children field.
                // processedCodeAction = codeAction.WrapResolvableCSharpCodeAction(context, LanguageServerConstants.CodeActions.FullyQualifyType);

                typeAccessibilityCodeActions = Array.Empty<CodeAction>();
                return false;
            }
            // For add using suggestions, the code action title is of the form:
            // `using System.Net;`
            else if (AddUsingsCodeActionProviderFactory.TryExtractNamespace(codeAction.Title, out var @namespace))
            {
                codeAction.Title = $"@using {@namespace}";
                processedCodeAction = codeAction.WrapResolvableCSharpCodeAction(context, LanguageServerConstants.CodeActions.AddUsing);
            }
            // Not a type accessibility code action
            else
            {
                typeAccessibilityCodeActions = Array.Empty<CodeAction>();
                return false;
            }

            typeAccessibilityCodeActions = new[] { processedCodeAction };
            return true;
        }

        private static CodeAction CreateFQNCodeAction(
            RazorCodeActionContext context,
            Diagnostic fqnDiagnostic,
            CodeAction codeAction,
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

            return new CodeAction()
            {
                Title = codeAction.Title,
                Edit = fqnWorkspaceEdit
            };
        }
    }
}
