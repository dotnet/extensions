// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [ExportLspMethod(Methods.TextDocumentCompletionResolveName)]
    internal class CompletionResolveHandler : IRequestHandler<CompletionItem, CompletionItem>
    {
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly LSPDocumentMappingProvider _documentMappingProvider;
        private readonly FormattingOptionsProvider _formattingOptionsProvider;
        private readonly CompletionRequestContextCache _completionRequestContextCache;

        [ImportingConstructor]
        public CompletionResolveHandler(
            LSPRequestInvoker requestInvoker,
            LSPDocumentMappingProvider documentMappingProvider,
            FormattingOptionsProvider formattingOptionsProvider,
            CompletionRequestContextCache completionRequestContextCache)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (documentMappingProvider is null)
            {
                throw new ArgumentNullException(nameof(documentMappingProvider));
            }

            if (formattingOptionsProvider is null)
            {
                throw new ArgumentNullException(nameof(formattingOptionsProvider));
            }

            if (completionRequestContextCache is null)
            {
                throw new ArgumentNullException(nameof(completionRequestContextCache));
            }

            _requestInvoker = requestInvoker;
            _documentMappingProvider = documentMappingProvider;
            _formattingOptionsProvider = formattingOptionsProvider;
            _completionRequestContextCache = completionRequestContextCache;
        }

        public async Task<CompletionItem> HandleRequestAsync(CompletionItem request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            if (request?.Data == null)
            {
                return request;
            }

            CompletionResolveData resolveData;
            if (request.Data is CompletionResolveData data)
            {
                resolveData = data;
            }
            else
            {
                resolveData = ((JToken)request.Data).ToObject<CompletionResolveData>();
            }

            // Set the original resolve data back so the language server deserializes it correctly.
            request.Data = resolveData.OriginalData;

            if (!_completionRequestContextCache.TryGet(resolveData.ResultId, out var requestContext))
            {
                // Could not find the associated request context.
                return request;
            }

            var serverContentType = requestContext.LanguageServerKind.ToContentType();
            var result = await _requestInvoker.ReinvokeRequestOnServerAsync<CompletionItem, CompletionItem>(
                Methods.TextDocumentCompletionResolveName,
                serverContentType,
                request,
                cancellationToken).ConfigureAwait(false);

            result = await PostProcessCompletionItemAsync(request, result, requestContext, cancellationToken).ConfigureAwait(false);
            return result;
        }

        private async Task<CompletionItem> PostProcessCompletionItemAsync(
            CompletionItem preResolveCompletionItem,
            CompletionItem resolvedCompletionItem,
            CompletionRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            // This is a special contract between the Visual Studio LSP platform and language servers where if insert text and text edit's are not present
            // then the "resolve" endpoint is guaranteed to run prior to a completion item's content being comitted. This gives language servers the
            // opportunity to lazily evaluate text edits which in turn we need to remap. Given text edits generated through this mechanism tend to be
            // more exntensive we do a full remapping gesture which includes formatting of said text-edits.
            var shouldRemapTextEdits = preResolveCompletionItem.InsertText == null && preResolveCompletionItem.TextEdit == null;
            if (!shouldRemapTextEdits)
            {
                // Don't need to remap anything, return early.
                return resolvedCompletionItem;
            }

            var formattingOptions = _formattingOptionsProvider.GetOptions(requestContext.HostDocumentUri);
            if (resolvedCompletionItem.TextEdit != null)
            {
                var containsSnippet = resolvedCompletionItem.InsertTextFormat == InsertTextFormat.Snippet;
                var remappedEdits = await _documentMappingProvider.RemapFormattedTextEditsAsync(
                    requestContext.ProjectedDocumentUri,
                    new[] { resolvedCompletionItem.TextEdit },
                    formattingOptions,
                    containsSnippet,
                    cancellationToken).ConfigureAwait(false);

                // We only passed in a single edit to be remapped
                var remappedEdit = remappedEdits.Single();
                resolvedCompletionItem.TextEdit = remappedEdit;
            }

            if (resolvedCompletionItem.AdditionalTextEdits != null)
            {
                var remappedEdits = await _documentMappingProvider.RemapFormattedTextEditsAsync(
                    requestContext.ProjectedDocumentUri,
                    resolvedCompletionItem.AdditionalTextEdits,
                    formattingOptions,
                    containsSnippet: false, // Additional text edits can't contain snippets
                    cancellationToken).ConfigureAwait(false);

                resolvedCompletionItem.AdditionalTextEdits = remappedEdits;
            }

            return resolvedCompletionItem;
        }
    }
}
