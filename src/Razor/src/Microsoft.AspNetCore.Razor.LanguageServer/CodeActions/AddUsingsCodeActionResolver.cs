// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions.Models;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    internal class AddUsingsCodeActionResolver : RazorCodeActionResolver
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;

        public AddUsingsCodeActionResolver(ForegroundDispatcher foregroundDispatcher, DocumentResolver documentResolver)
        {
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
        }

        public override string Action => LanguageServerConstants.CodeActions.AddUsing;

        public override async Task<WorkspaceEdit> ResolveAsync(JObject data, CancellationToken cancellationToken)
        {
            if (data is null)
            {
                return null;
            }

            var actionParams = data.ToObject<AddUsingsCodeActionParams>();
            if (actionParams is null)
            {
                return null;
            }

            var path = actionParams.Uri.GetAbsoluteOrUNCPath();

            var document = await Task.Factory.StartNew(() =>
            {
                _documentResolver.TryResolveDocument(path, out var documentSnapshot);
                return documentSnapshot;
            }, cancellationToken, TaskCreationOptions.None, _foregroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
            if (document is null)
            {
                return null;
            }

            var text = await document.GetTextAsync().ConfigureAwait(false);
            if (text is null)
            {
                return null;
            }

            var codeDocument = await document.GetGeneratedOutputAsync().ConfigureAwait(false);
            if (codeDocument.IsUnsupported())
            {
                return null;
            }

            var codeDocumentIdentifier = new VersionedTextDocumentIdentifier() { Uri = actionParams.Uri };
            return CreateAddUsingWorkspaceEdit(actionParams.Namespace, codeDocument, codeDocumentIdentifier);
        }

        internal static WorkspaceEdit CreateAddUsingWorkspaceEdit(string @namespace, RazorCodeDocument codeDocument, VersionedTextDocumentIdentifier codeDocumentIdentifier)
        {
            /* The heuristic is as follows:
             *
             * - If no @using, @namespace, or @page directives are present, insert the statements at the top of the
             *   file in alphabetical order.
             * - If a @namespace or @page are present, the statements are inserted after the last line-wise in
             *   alphabetical order.
             * - If @using directives are present and alphabetized with System directives at the top, the statements
             *   will be placed in the correct locations according to that ordering.
             * - Otherwise it's kinda undefined; it's only geared to insert based on alphabetization.
             *
             * This is generally sufficient for our current situation (inserting a single @using statement to include a
             * component), however it has holes if we eventually use it for other purposes. If we want to deal with
             * that now I can come up with a more sophisticated heuristic (something along the lines of checking if
             * there's already an ordering, etc.).
             */
            var documentChanges = new List<WorkspaceEditDocumentChange>();
            var usingDirectives = FindUsingDirectives(codeDocument);
            if (usingDirectives.Count > 0)
            {
                // Interpolate based on existing @using statements
                var edits = GenerateSingleUsingEditsInterpolated(codeDocument, codeDocumentIdentifier, @namespace, usingDirectives);
                documentChanges.Add(edits);
            }
            else
            {
                // Just throw them at the top
                var edits = GenerateSingleUsingEditsAtTop(codeDocument, codeDocumentIdentifier, @namespace);
                documentChanges.Add(edits);
            }

            return new WorkspaceEdit()
            {
                DocumentChanges = documentChanges
            };
        }

        private static WorkspaceEditDocumentChange GenerateSingleUsingEditsInterpolated(
            RazorCodeDocument codeDocument,
            VersionedTextDocumentIdentifier codeDocumentIdentifier,
            string newUsingNamespace,
            List<RazorUsingDirective> existingUsingDirectives)
        {
            var edits = new List<TextEdit>();
            var newText = $"@using {newUsingNamespace}{Environment.NewLine}";

            foreach (var usingDirective in existingUsingDirectives)
            {
                // Skip System directives; if they're at the top we don't want to insert before them
                var usingDirectiveNamespace = usingDirective.Statement.ParsedNamespace;
                if (usingDirectiveNamespace.StartsWith("System", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.CompareOrdinal(newUsingNamespace, usingDirectiveNamespace) < 0)
                {
                    var usingDirectiveLineIndex = codeDocument.Source.Lines.GetLocation(usingDirective.Node.Span.Start).LineIndex;
                    var head = new Position(usingDirectiveLineIndex, 0);
                    var edit = new TextEdit() { Range = new Range(head, head), NewText = newText };
                    edits.Add(edit);
                    break;
                }
            }

            // If we haven't actually found a place to insert the using directive, do so at the end
            if (edits.Count == 0)
            {
                var endIndex = existingUsingDirectives.Last().Node.Span.End;
                var lineIndex = GetLineIndexOrEnd(codeDocument, endIndex - 1) + 1;
                var head = new Position(lineIndex, 0);
                var edit = new TextEdit() { Range = new Range(head, head), NewText = newText };
                edits.Add(edit);
            }

            return new WorkspaceEditDocumentChange(new TextDocumentEdit()
            {
                TextDocument = codeDocumentIdentifier,
                Edits = edits,
            });
        }

        private static WorkspaceEditDocumentChange GenerateSingleUsingEditsAtTop(
            RazorCodeDocument codeDocument,
            VersionedTextDocumentIdentifier codeDocumentIdentifier,
            string newUsingNamespace)
        {
            var head = new Position(0, 0);

            // If we don't have usings, insert after the last namespace or page directive, which ever comes later
            var syntaxTreeRoot = codeDocument.GetSyntaxTree().Root;
            var lastNamespaceOrPageDirective = syntaxTreeRoot
                .DescendantNodes()
                .Where(n => IsNamespaceOrPageDirective(n))
                .LastOrDefault();
            if (lastNamespaceOrPageDirective != null)
            {
                var lineIndex = GetLineIndexOrEnd(codeDocument, lastNamespaceOrPageDirective.Span.End - 1) + 1;
                head = new Position(lineIndex, 0);
            }

            // Insert all usings at the given point
            var range = new Range(head, head);
            return new WorkspaceEditDocumentChange(new TextDocumentEdit
            {
                TextDocument = codeDocumentIdentifier,
                Edits = new[]
                {
                    new TextEdit()
                    {
                        NewText = string.Concat($"@using {newUsingNamespace}{Environment.NewLine}"),
                        Range = range,
                    }
                }
            });
        }

        private static int GetLineIndexOrEnd(RazorCodeDocument codeDocument, int endIndex)
        {
            if (endIndex < codeDocument.Source.Length)
            {
                return codeDocument.Source.Lines.GetLocation(endIndex).LineIndex;
            }
            else
            {
                return codeDocument.Source.Lines.Count;
            }
        }

        private static List<RazorUsingDirective> FindUsingDirectives(RazorCodeDocument codeDocument)
        {
            var directives = new List<RazorUsingDirective>();
            var syntaxTreeRoot = codeDocument.GetSyntaxTree().Root;
            foreach (var node in syntaxTreeRoot.DescendantNodes())
            {
                if (node is RazorDirectiveSyntax directiveNode)
                {
                    foreach (var child in directiveNode.DescendantNodes())
                    {
                        var context = child.GetSpanContext();
                        if (context != null && context.ChunkGenerator is AddImportChunkGenerator usingStatement && !usingStatement.IsStatic)
                        {
                            directives.Add(new RazorUsingDirective(directiveNode, usingStatement));
                        }
                    }
                }
            }
            return directives;
        }

        private static bool IsNamespaceOrPageDirective(SyntaxNode node)
        {
            if (node is RazorDirectiveSyntax directiveNode)
            {
                return directiveNode.DirectiveDescriptor == ComponentPageDirective.Directive || directiveNode.DirectiveDescriptor == NamespaceDirective.Directive;
            }
            return false;
        }

        private struct RazorUsingDirective
        {
            readonly public RazorDirectiveSyntax Node { get; }
            readonly public AddImportChunkGenerator Statement { get; }

            public RazorUsingDirective(RazorDirectiveSyntax node, AddImportChunkGenerator statement)
            {
                Node = node;
                Statement = statement;
            }
        }
    }
}
