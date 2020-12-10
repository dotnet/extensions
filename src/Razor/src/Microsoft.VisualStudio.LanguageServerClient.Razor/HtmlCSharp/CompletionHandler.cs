// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentCompletionName)]
    internal class CompletionHandler : IRequestHandler<CompletionParams, SumType<CompletionItem[], CompletionList>?>
    {
        private static readonly IReadOnlyList<string> RazorTriggerCharacters = new[] { "@" };
        private static readonly IReadOnlyList<string> CSharpTriggerCharacters = new[] { " ", "(", "=", "#", ".", "<", "[", "{", "\"", "/", ":", ">", "~" };
        private static readonly IReadOnlyList<string> HtmlTriggerCharacters = new[] { ":", "@", "#", ".", "!", "*", ",", "(", "[", "=", "_", "-", "<", "&", "\\", "/", "'", "\"", "=", ":", " " };

        public static readonly IReadOnlyList<string> AllTriggerCharacters = new HashSet<string>(
            CSharpTriggerCharacters
                .Concat(HtmlTriggerCharacters)
                .Concat(RazorTriggerCharacters))
            .ToArray();

        private static readonly IReadOnlyCollection<string> Keywords = new string[] {
            "for", "foreach", "while", "switch", "lock",
            "case", "if", "try", "do", "using"
        };

        private static readonly IReadOnlyCollection<CompletionItem> KeywordCompletionItems = Keywords.Select(k => new CompletionItem
        {
            Label = k,
            InsertText = k,
            FilterText = k,
            Kind = CompletionItemKind.Keyword,
            SortText = k,
            InsertTextFormat = InsertTextFormat.Plaintext,
        }).ToArray();

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
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (clientCapabilities is null)
            {
                throw new ArgumentNullException(nameof(clientCapabilities));
            }

            if (!_documentManager.TryGetDocument(request.TextDocument.Uri, out var documentSnapshot))
            {
                return null;
            }

            var projectionResult = await _projectionProvider.GetProjectionAsync(documentSnapshot, request.Position, cancellationToken).ConfigureAwait(false);
            if (projectionResult == null)
            {
                return null;
            }

            var serverKind = projectionResult.LanguageKind == RazorLanguageKind.CSharp ? LanguageServerKind.CSharp : LanguageServerKind.Html;

            var (succeeded, result) = await TryGetProvisionalCompletionsAsync(request, documentSnapshot, projectionResult, cancellationToken).ConfigureAwait(false);
            if (succeeded)
            {
                // This means the user has just typed a dot after some identifier such as (cursor is pipe): "DateTime.| "
                // In this case Razor interprets after the dot as Html and before it as C#.
                // We use this criteria to provide a better completion experience for what we call provisional changes.
            }
            else if (!TriggerAppliesToProjection(request.Context, projectionResult.LanguageKind))
            {
                return null;
            }
            else
            {
                var completionContext = RewriteContext(request.Context, projectionResult.LanguageKind);

                // This is a valid non-provisional completion request.
                var completionParams = new CompletionParams()
                {
                    Context = completionContext,
                    Position = projectionResult.Position,
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = projectionResult.Uri
                    }
                };

                result = await _requestInvoker.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(
                    Methods.TextDocumentCompletionName,
                    serverKind.ToContentType(),
                    completionParams,
                    cancellationToken).ConfigureAwait(false);
            }

            if (result.HasValue)
            {
                // Set some context on the CompletionItem so the CompletionResolveHandler can handle it accordingly.
                result = SetResolveData(result.Value, serverKind);
                if (serverKind == LanguageServerKind.CSharp && string.Equals(request.Context.TriggerCharacter, "@", StringComparison.Ordinal))
                {
                    result = DoNotPreselect(result.Value);
                    result = IncludeCSharpKeywords(result.Value, serverKind);
                }
            }

            return result;
        }

        private CompletionContext RewriteContext(CompletionContext context, RazorLanguageKind languageKind)
        {
            if (context.TriggerKind != CompletionTriggerKind.TriggerCharacter)
            {
                // Non-triggered based completion, the existing context is valid;
                return context;
            }

            if (languageKind == RazorLanguageKind.CSharp && CSharpTriggerCharacters.Contains(context.TriggerCharacter))
            {
                // C# trigger character for C# content
                return context;
            }

            if (languageKind == RazorLanguageKind.Html && HtmlTriggerCharacters.Contains(context.TriggerCharacter))
            {
                // HTML trigger character for HTML content
                return context;
            }

            // Trigger character not associated with the current langauge. Transform the context into an invoked context.
            var rewrittenContext = new VSCompletionContext()
            {
                TriggerKind = CompletionTriggerKind.Invoked,
            };

            var invokeKind = (context as VSCompletionContext)?.InvokeKind;
            if (invokeKind.HasValue)
            {
                rewrittenContext.InvokeKind = invokeKind.Value;
            }

            return rewrittenContext;
        }

        internal async Task<(bool, SumType<CompletionItem[], CompletionList>?)> TryGetProvisionalCompletionsAsync(CompletionParams request, LSPDocumentSnapshot documentSnapshot, ProjectionResult projection, CancellationToken cancellationToken)
        {
            SumType<CompletionItem[], CompletionList>? result = null;
            if (projection.LanguageKind != RazorLanguageKind.Html ||
                request.Context.TriggerKind != CompletionTriggerKind.TriggerCharacter ||
                request.Context.TriggerCharacter != ".")
            {
                return (false, result);
            }

            if (projection.Position.Character == 0)
            {
                // We're at the start of line. Can't have provisional completions here.
                return (false, result);
            }

            var previousCharacterPosition = new Position(projection.Position.Line, projection.Position.Character - 1);
            var previousCharacterProjection = await _projectionProvider.GetProjectionAsync(documentSnapshot, previousCharacterPosition, cancellationToken).ConfigureAwait(false);
            if (previousCharacterProjection == null || previousCharacterProjection.LanguageKind != RazorLanguageKind.CSharp || previousCharacterProjection.HostDocumentVersion is null)
            {
                return (false, result);
            }

            if (!(_documentManager is TrackingLSPDocumentManager trackingDocumentManager))
            {
                return (false, result);
            }

            // Edit the CSharp projected document to contain a '.'. This allows C# completion to provide valid
            // completion items for moments when a user has typed a '.' that's typically interpreted as Html.
            var addProvisionalDot = new VisualStudioTextChange(previousCharacterProjection.PositionIndex, 0, ".");

            await _joinableTaskFactory.SwitchToMainThreadAsync();

            trackingDocumentManager.UpdateVirtualDocument<CSharpVirtualDocument>(documentSnapshot.Uri, new[] { addProvisionalDot }, previousCharacterProjection.HostDocumentVersion.Value);

            var provisionalCompletionParams = new CompletionParams()
            {
                Context = request.Context,
                Position = new Position(previousCharacterProjection.Position.Line, previousCharacterProjection.Position.Character + 1),
                TextDocument = new TextDocumentIdentifier() { Uri = previousCharacterProjection.Uri }
            };

            result = await _requestInvoker.ReinvokeRequestOnServerAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(
                Methods.TextDocumentCompletionName,
                RazorLSPConstants.CSharpContentTypeName,
                provisionalCompletionParams,
                cancellationToken).ConfigureAwait(true);

            // We have now obtained the necessary completion items. We no longer need the provisional change. Revert.
            var removeProvisionalDot = new VisualStudioTextChange(previousCharacterProjection.PositionIndex, 1, string.Empty);

            trackingDocumentManager.UpdateVirtualDocument<CSharpVirtualDocument>(documentSnapshot.Uri, new[] { removeProvisionalDot }, previousCharacterProjection.HostDocumentVersion.Value);

            return (true, result);
        }

        // In cases like "@{" preselection can lead to unexpected behavior, so let's exclude it.
        private SumType<CompletionItem[], CompletionList>? DoNotPreselect(SumType<CompletionItem[], CompletionList> completionResult)
        {
            var result = completionResult.Match<SumType<CompletionItem[], CompletionList>?>(
                items => {
                    foreach (var i in items)
                    {
                        i.Preselect = false;
                    }

                    return items;
                },
                list => {
                    foreach (var i in list.Items)
                    {
                        i.Preselect = false;
                    }

                    return list;
                });

            return result;
        }

        // C# keywords were previously provided by snippets, but as of now C# LSP doesn't provide snippets. 
        // We're providing these for now to improve the user experience (not having to ESC out of completions to finish),
        // but once C# starts providing them their completion will be offered instead, at which point we should be able to remove this step.
        private SumType<CompletionItem[], CompletionList>? IncludeCSharpKeywords(SumType<CompletionItem[], CompletionList> completionResult, LanguageServerKind kind)
        {
            var result = completionResult.Match<SumType<CompletionItem[], CompletionList>?>(
                items =>
                {
                    var newList = items.Union(KeywordCompletionItems, CompletionItemComparer.Instance);
                    return newList.ToArray();
                },
                list =>
                {
                    var newList = list.Items.Union(KeywordCompletionItems, CompletionItemComparer.Instance);
                    list.Items = newList.ToArray();

                    return list;
                });

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
                    if (list is VSCompletionList vsList)
                    {
                        return new VSCompletionList()
                        {
                            Items = newItems,
                            IsIncomplete = vsList.IsIncomplete,
                            SuggestionMode = vsList.SuggestionMode,
                            ContinueCharacters = vsList.ContinueCharacters
                        };
                    }

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

        // Internal for testing
        internal bool TriggerAppliesToProjection(CompletionContext context, RazorLanguageKind languageKind)
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

        private static bool IsApplicableTriggerCharacter(string triggerCharacter, RazorLanguageKind languageKind)
        {
            if (RazorTriggerCharacters.Contains(triggerCharacter))
            {
                // Razor trigger characters always transition into either C# or HTML, always note as "applicable".
                return true;
            }
            else if (languageKind == RazorLanguageKind.CSharp)
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

        private class CompletionItemComparer : IEqualityComparer<CompletionItem>
        {
            public static CompletionItemComparer Instance = new CompletionItemComparer();

            public bool Equals(CompletionItem x, CompletionItem y)
            {
                if (x is null && y is null)
                {
                    return true;
                }
                else if (x is null || y is null)
                {
                    return false;
                }

                return x.Label.Equals(y.Label, StringComparison.Ordinal);
            }

            public int GetHashCode(CompletionItem obj) => obj?.Label?.GetHashCode() ?? 0;
        }
    }
}
