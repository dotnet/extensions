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
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Microsoft.AspNetCore.Razor.LanguageServer.CodeActions
{
    class AddUsingsCodeActionResolver : RazorCodeActionResolver
    {
        private readonly ForegroundDispatcher _foregroundDispatcher;
        private readonly DocumentResolver _documentResolver;

        public AddUsingsCodeActionResolver(ForegroundDispatcher foregroundDispatcher, DocumentResolver documentResolver)
        {
            _foregroundDispatcher = foregroundDispatcher ?? throw new ArgumentNullException(nameof(foregroundDispatcher));
            _documentResolver = documentResolver ?? throw new ArgumentNullException(nameof(documentResolver));
        }

        public override string Action => LanguageServerConstants.CodeActions.AddUsings;

        public override async Task<WorkspaceEdit> ResolveAsync(JObject data, CancellationToken cancellationToken)
        {
            var actionParams = data.ToObject<AddUsingsCodeActionParams>();
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

            if (!FileKinds.IsComponent(codeDocument.GetFileKind()))
            {
                return null;
            }

            var codeDocumentIdentifier = new VersionedTextDocumentIdentifier() { Uri = actionParams.Uri, Version = 0 };
            var documentChanges = new List<WorkspaceEditDocumentChange>();

            var namespaceList = actionParams.Namespaces;

            var usingDirectives = FindUsingDirectives(codeDocument);
            if (usingDirectives.Count > 0)
            {
                documentChanges.Add(GenerateUsingEditsInterpolated(codeDocument, codeDocumentIdentifier, actionParams.Namespaces, usingDirectives));
            }
            else
            {
                documentChanges.Add(GenerateUsingEditsAtTop(codeDocument, codeDocumentIdentifier, actionParams.Namespaces));
            }

            return new WorkspaceEdit()
            {
                DocumentChanges = documentChanges
            };
        }

        private static WorkspaceEditDocumentChange GenerateUsingEditsInterpolated(
            RazorCodeDocument codeDocument,
            VersionedTextDocumentIdentifier codeDocumentIdentifier,
            string[] namespaceArray,
            List<RazorUsingDirective> usingDirectives)
        {
            // Sort and queue namespaces
            var namespaceList = namespaceArray.ToList();
            namespaceList.Sort();
            var namespaces = new Queue<string>(namespaceList);

            var edits = new List<TextEdit>();
            foreach (var usingDirective in usingDirectives)
            {
                // Break early if we're done
                if (namespaces.Count == 0)
                {
                    break;
                }

                // Skip using directives 
                var usingDirectiveNamespace = usingDirective.Statement.ParsedNamespace;
                if (usingDirectiveNamespace.StartsWith("System") || usingDirectiveNamespace.Contains("="))
                {
                    continue;
                }

                // Insert all using directives that fit before the next using
                if (namespaces.Peek().CompareTo(usingDirectiveNamespace) < 0)
                {
                    var usingDirectiveLineIndex = codeDocument.Source.Lines.GetLocation(usingDirective.Node.Span.Start).LineIndex;
                    var head = new Position(usingDirectiveLineIndex, 0);
                    var edit = new TextEdit() { Range = new Range(head, head), NewText = "" };
                    do
                    {
                        edit.NewText += $"@using {namespaces.Dequeue()}{Environment.NewLine}";
                    } while (namespaces.Count > 0 && namespaces.Peek().CompareTo(usingDirectiveNamespace) < 0);
                    edits.Add(edit);
                }
            }

            // Add the remaining usings to the end
            if (namespaces.Count > 0)
            {
                var endIndex = usingDirectives.Last().Node.Span.End;
                var lineIndex = GetLineIndexOrEnd(codeDocument, endIndex - 1) + 1;
                var head = new Position(lineIndex, 0);
                var edit = new TextEdit() { Range = new Range(head, head), NewText = "" };
                do
                {
                    edit.NewText += $"@using {namespaces.Dequeue()}{Environment.NewLine}";
                } while (namespaces.Count > 0);
                edits.Add(edit);
            }

            return new WorkspaceEditDocumentChange(new TextDocumentEdit()
            {
                TextDocument = codeDocumentIdentifier,
                Edits = edits,
            });
        }

        private static WorkspaceEditDocumentChange GenerateUsingEditsAtTop(
            RazorCodeDocument codeDocument,
            VersionedTextDocumentIdentifier codeDocumentIdentifier,
            string[] namespaceArray)
        {
            var namespaceList = namespaceArray.ToList();
            namespaceList.Sort();

            // If we don't have usings, insert after the last namespace or page directive, which ever comes later
            var head = new Position(1, 0);
            var lastNamespaceOrPageDirective = codeDocument.GetSyntaxTree().Root
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
                        NewText = string.Concat(namespaceList.Select(n => $"@using {n}{Environment.NewLine}")),
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
            foreach (var node in codeDocument.GetSyntaxTree().Root.DescendantNodes())
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
