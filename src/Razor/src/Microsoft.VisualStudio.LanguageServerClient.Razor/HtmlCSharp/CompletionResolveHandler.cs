// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
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

        [ImportingConstructor]
        public CompletionResolveHandler(LSPRequestInvoker requestInvoker)
        {
            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            _requestInvoker = requestInvoker;
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

            if (resolveData.LanguageServerKind != LanguageServerKind.CSharp)
            {
                // We currently only want to resolve C# completion items.
                return request;
            }

            var serverContentType = resolveData.LanguageServerKind.ToContentType();
            var result = await _requestInvoker.ReinvokeRequestOnServerAsync<CompletionItem, CompletionItem>(
                Methods.TextDocumentCompletionResolveName,
                serverContentType,
                request,
                cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
