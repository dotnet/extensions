// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using System.Diagnostics;

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

            var results = context.SupportsCodeActionResolve ?
                ProcessCodeActionsVS(context, codeActions) :
                ProcessCodeActionsVSCode(context, codeActions);

            var orderedResults = results.OrderBy(codeAction => codeAction.Title).ToArray();
            return Task.FromResult(orderedResults as IReadOnlyList<RazorCodeAction>);
        }

        private static IEnumerable<RazorCodeAction> ProcessCodeActionsVSCode(
            RazorCodeActionContext context,
            IEnumerable<RazorCodeAction> codeActions)
        {
            var diagnostics = context.Request.Context.Diagnostics.Where(diagnostic =>
                diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Code?.IsString == true &&
                SupportedDiagnostics.Any(d => diagnostic.Code.Value.String.Equals(d, StringComparison.OrdinalIgnoreCase)));

            if (diagnostics is null || !diagnostics.Any())
            {
                return Array.Empty<RazorCodeAction>();
            }

            var typeAccessibilityCodeActions = new List<RazorCodeAction>();

            foreach (var diagnostic in diagnostics)
            {
                // Corner case handling for diagnostics which (momentarily) linger after
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

                foreach (var codeAction in codeActions)
                {
                    if (!codeAction.Name.Equals(LanguageServerConstants.CodeActions.CodeActionFromVSCode, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var associatedValue = context.SourceText.GetSubTextString(diagnosticSpan);

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
                        continue;
                    }

                    var fqnCodeAction = CreateFQNCodeAction(context, diagnostic, codeAction, fqn);
                    typeAccessibilityCodeActions.Add(fqnCodeAction);

                    var addUsingCodeAction = AddUsingsCodeActionProviderFactory.CreateAddUsingCodeAction(fqn, context.Request.TextDocument.Uri);
                    if (addUsingCodeAction != null)
                    {
                        typeAccessibilityCodeActions.Add(addUsingCodeAction);
                    }
                }
            }

            return typeAccessibilityCodeActions;
        }

        private static IEnumerable<RazorCodeAction> ProcessCodeActionsVS(
            RazorCodeActionContext context,
            IEnumerable<RazorCodeAction> codeActions)
        {
            var typeAccessibilityCodeActions = new List<RazorCodeAction>(1);

            foreach (var codeAction in codeActions)
            {
                if (codeAction.Name.Equals(RazorPredefinedCodeFixProviderNames.FullyQualify, StringComparison.Ordinal))
                {
                    var node = FindImplicitOrExplicitExpressionNode(context);
                    string action;

                    // The formatting pass of our Default code action resolver rejects
                    // implicit/explicit expressions. So if we're in an implicit expression,
                    // we run the remapping resolver responsible for simply remapping
                    // (without formatting) the resolved code action. We do not support
                    // explicit expressions due to issues with the remapping methodology
                    // risking document corruption.
                    if (node is null)
                    {
                        action = LanguageServerConstants.CodeActions.Default;
                    }
                    else if (node is CSharpImplicitExpressionSyntax)
                    {
                        action = LanguageServerConstants.CodeActions.UnformattedRemap;
                    }
                    else
                    {
                        continue;
                    }

                    typeAccessibilityCodeActions.Add(codeAction.WrapResolvableCSharpCodeAction(context, action));
                }
                // For add using suggestions, the code action title is of the form:
                // `using System.Net;`
                else if (codeAction.Name.Equals(RazorPredefinedCodeFixProviderNames.AddImport, StringComparison.Ordinal) &&
                    AddUsingsCodeActionProviderFactory.TryExtractNamespace(codeAction.Title, out var @namespace))
                {
                    codeAction.Title = $"@using {@namespace}";
                    typeAccessibilityCodeActions.Add(codeAction.WrapResolvableCSharpCodeAction(context, LanguageServerConstants.CodeActions.AddUsing));
                }
                // Not a type accessibility code action
                else
                {
                    continue;
                }
            }

            return typeAccessibilityCodeActions;

            static SyntaxNode FindImplicitOrExplicitExpressionNode(RazorCodeActionContext context)
            {
                var change = new SourceChange(context.Location.AbsoluteIndex, length: 0, newText: string.Empty);
                var syntaxTree = context.CodeDocument.GetSyntaxTree();
                if (syntaxTree?.Root is null)
                {
                    return null;
                }

                var owner = syntaxTree.Root.LocateOwner(change);
                if (owner == null)
                {
                    Debug.Fail("Owner should never be null.");
                    return null;
                }

                // E.g, (| is position)
                //
                // `@|foo` - true
                // `@(|foo)` - true
                //
                return owner.AncestorsAndSelf().FirstOrDefault(n => n is CSharpImplicitExpressionSyntax || n is CSharpExplicitExpressionSyntax);
            }
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
    }
}
