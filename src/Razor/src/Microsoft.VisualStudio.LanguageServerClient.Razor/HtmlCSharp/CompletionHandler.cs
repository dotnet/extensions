// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentCompletionName)]
    internal class CompletionHandler : IRequestHandler<CompletionParams, SumType<CompletionItem[], CompletionList>?>
    {
        private static readonly IReadOnlyList<string> CSharpTriggerCharacters = new[] { ".", "@" };
        private static readonly IReadOnlyList<string> HtmlTriggerCharacters = new[] { "<", "&", "\\", "/", "'", "\"", "=", ":" };
        private static readonly IReadOnlyList<string> AllTriggerCharacters = CSharpTriggerCharacters.Concat(HtmlTriggerCharacters).ToArray();

        private readonly JoinableTaskFactory _joinableTaskFactory;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentManager _documentManager;
        private readonly LSPProjectionProvider _projectionProvider;

        [ImportingConstructor]
        public CompletionHandler(
            JoinableTaskContext joinableTaskContext,
            LSPRequestInvoker requestInvoker,
            LSPDocumentManager documentManager,
            LSPProjectionProvider projectionProvider)
        {
            if (joinableTaskContext is null)
            {
                throw new ArgumentNullException(nameof(joinableTaskContext));
            }

            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (documentManager is null)
            {
                throw new ArgumentNullException(nameof(documentManager));
            }

            if (projectionProvider is null)
            {
                throw new ArgumentNullException(nameof(projectionProvider));
            }

            _joinableTaskFactory = joinableTaskContext.Factory;
            _requestInvoker = requestInvoker;
            _documentManager = documentManager;
            _projectionProvider = projectionProvider;
        }

        public async Task<SumType<CompletionItem[], CompletionList>?> HandleRequestAsync(CompletionParams request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            await _joinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return null;
            }

            // Switch to a background thread.
            await TaskScheduler.Default;

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken);
            if (projectionResult == null)
            {
                return null;
            }

            if (!TriggerAppliesToProjection(request.Context, projectionResult.LanguageKind))
            {
                return null;
            }

            var completionParams = new CompletionParams()
            {
                Context = request.Context,
                Position = projectionResult.Position,
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = projectionResult.Uri
                }
            };

            var serverKind = projectionResult.LanguageKind == RazorLanguageKind.CSharp ? LanguageServerKind.CSharp : LanguageServerKind.Html;
            var result = await _requestInvoker.RequestServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(
                Methods.TextDocumentCompletionName,
                serverKind,
                completionParams,
                cancellationToken);

            if (result.HasValue)
            {
                // Set some context on the CompletionItem so the CompletionResolveHandler can handle it accordingly.
                result = SetResolveData(result.Value, serverKind);
            }

            return result;
        }

        // Internal for testing
        internal SumType<CompletionItem[], CompletionList>? SetResolveData(SumType<CompletionItem[], CompletionList> completionResult, LanguageServerKind kind)
        {
            var result = completionResult.Match<SumType<CompletionItem[], CompletionList>?>(
                items =>
                {
                    var newItems = items.Select(item => SetData(item)).ToArray();
                    return newItems;
                },
                list =>
                {
                    var newItems = list.Items.Select(item => SetData(item)).ToArray();
                    return new CompletionList()
                    {
                        Items = newItems,
                        IsIncomplete = list.IsIncomplete,
                    };
                },
                () => null);

            return result;

            CompletionItem SetData(CompletionItem item)
            {
                var data = new CompletionResolveData()
                {
                    LanguageServerKind = kind,
                    OriginalData = item.Data
                };

                item.Data = data;

                return item;
            }
        }

        private bool TriggerAppliesToProjection(CompletionContext context, RazorLanguageKind languageKind)
        {
            if (languageKind == RazorLanguageKind.Razor)
            {
                // We don't handle any type of triggers in Razor pieces of the document
                return false;
            }

            if (context.TriggerKind != CompletionTriggerKind.TriggerCharacter)
            {
                // Not a trigger character completion, allow it.
                return true;
            }

            if (!AllTriggerCharacters.Contains(context.TriggerCharacter))
            {
                // This is an auto-invoked completion from the VS LSP platform. Completions are automatically invoked upon typing identifiers
                // and are represented as CompletionTriggerKind.TriggerCharacter and have a trigger character that we have not registered for.
                return true;
            }

            if (IsApplicableTriggerCharacter(context.TriggerCharacter, languageKind))
            {
                // Trigger character is associated with the langauge at the current cursor position
                return true;
            }

            // We were triggered but the trigger character doesn't make sense for the current cursor position. Bail.
            return false;
        }

        private bool IsApplicableTriggerCharacter(string triggerCharacter, RazorLanguageKind languageKind)
        {
            if (languageKind == RazorLanguageKind.CSharp)
            {
                return CSharpTriggerCharacters.Contains(triggerCharacter);
            }
            else if (languageKind == RazorLanguageKind.Html)
            {
                return HtmlTriggerCharacters.Contains(triggerCharacter);
            }

            // Unknown trigger character.
            return false;
        }
    }
}
