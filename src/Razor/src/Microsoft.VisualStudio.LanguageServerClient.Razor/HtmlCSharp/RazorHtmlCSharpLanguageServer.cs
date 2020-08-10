// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class RazorHtmlCSharpLanguageServer : IDisposable
    {
        private readonly JsonRpc _jsonRpc;
        private readonly ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> _requestHandlers;
        private VSClientCapabilities _clientCapabilities;

        public RazorHtmlCSharpLanguageServer(
            Stream inputStream,
            Stream outputStream,
            IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers) : this(requestHandlers)
        {
            if (inputStream is null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (outputStream is null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            _jsonRpc = new JsonRpc(outputStream, inputStream, this);
            _jsonRpc.StartListening();
        }

        // Test constructor
        internal RazorHtmlCSharpLanguageServer(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
        {
            if (requestHandlers is null)
            {
                throw new ArgumentNullException(nameof(requestHandlers));
            }

            _requestHandlers = CreateMethodToHandlerMap(requestHandlers);
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public Task<InitializeResult> InitializeAsync(JToken input, CancellationToken cancellationToken)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            // InitializeParams only references ClientCapabilities, but the VS LSP client
            // sends additional VS specific capabilities, so directly deserialize them into the VSClientCapabilities
            // to avoid losing them.
            _clientCapabilities = input["capabilities"].ToObject<VSClientCapabilities>();
            var initializeParams = input.ToObject<InitializeParams>();
            return ExecuteRequestAsync<InitializeParams, InitializeResult>(Methods.InitializeName, initializeParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public Task ShutdownAsync(CancellationToken cancellationToken)
        {
            // Nothing to detatch to yet.

            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.ExitName)]
        public Task ExitAsync(CancellationToken cancellationToken)
        {
            Dispose();

            return Task.CompletedTask;
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionName, UseSingleObjectParameterDeserialization =  true)]
        public Task<SumType<CompletionItem[], CompletionList>?> ProvideCompletionsAsync(CompletionParams completionParams, CancellationToken cancellationToken)
        {
            if (completionParams is null)
            {
                throw new ArgumentNullException(nameof(completionParams));
            }

            return ExecuteRequestAsync<CompletionParams, SumType<CompletionItem[], CompletionList>?>(Methods.TextDocumentCompletionName, completionParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentHoverName, UseSingleObjectParameterDeserialization = true)]
        public Task<Hover> ProvideHoverAsync(TextDocumentPositionParams positionParams, CancellationToken cancellationToken)
        {
            if (positionParams is null)
            {
                throw new ArgumentNullException(nameof(positionParams));
            }

            return ExecuteRequestAsync<TextDocumentPositionParams, Hover>(Methods.TextDocumentHoverName, positionParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionResolveName, UseSingleObjectParameterDeserialization = true)]
        public Task<CompletionItem> ResolveCompletionAsync(CompletionItem request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return ExecuteRequestAsync<CompletionItem, CompletionItem>(Methods.TextDocumentCompletionResolveName, request, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.OnAutoInsertName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentOnAutoInsertResponseItem> OnAutoInsertAsync(DocumentOnAutoInsertParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return ExecuteRequestAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem>(MSLSPMethods.OnAutoInsertName, request, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentOnTypeFormattingName, UseSingleObjectParameterDeserialization = true)]
        public Task<TextEdit[]> OnTypeFormattingAsync(DocumentOnTypeFormattingParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return ExecuteRequestAsync<DocumentOnTypeFormattingParams, TextEdit[]>(Methods.TextDocumentOnTypeFormattingName, request, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDefinitionName, UseSingleObjectParameterDeserialization = true)]
        public Task<Location[]> GoToDefinitionAsync(TextDocumentPositionParams positionParams, CancellationToken cancellationToken)
        {
            if (positionParams is null)
            {
                throw new ArgumentNullException(nameof(positionParams));
            }

            return ExecuteRequestAsync<TextDocumentPositionParams, Location[]>(Methods.TextDocumentDefinitionName, positionParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentReferencesName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSReferenceItem[]> FindAllReferencesAsync(ReferenceParams referenceParams, CancellationToken cancellationToken)
        {
            if (referenceParams is null)
            {
                throw new ArgumentNullException(nameof(referenceParams));
            }

            return ExecuteRequestAsync<ReferenceParams, VSReferenceItem[]>(Methods.TextDocumentReferencesName, referenceParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentSignatureHelpName, UseSingleObjectParameterDeserialization = true)]
        public Task<SignatureHelp> SignatureHelpAsync(TextDocumentPositionParams positionParams, CancellationToken cancellationToken)
        {
            if (positionParams is null)
            {
                throw new ArgumentNullException(nameof(positionParams));
            }

            return ExecuteRequestAsync<TextDocumentPositionParams, SignatureHelp>(Methods.TextDocumentSignatureHelpName, positionParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentDocumentHighlightName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentHighlight[]> HighlightDocumentAsync(DocumentHighlightParams documentHighlightParams, CancellationToken cancellationToken)
        {
            if (documentHighlightParams is null)
            {
                throw new ArgumentNullException(nameof(documentHighlightParams));
            }

            return ExecuteRequestAsync<DocumentHighlightParams, DocumentHighlight[]>(Methods.TextDocumentDocumentHighlightName, documentHighlightParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentRenameName, UseSingleObjectParameterDeserialization = true)]
        public Task<WorkspaceEdit> RenameAsync(RenameParams renameParams, CancellationToken cancellationToken)
        {
            if (renameParams is null)
            {
                throw new ArgumentNullException(nameof(renameParams));
            }

            return ExecuteRequestAsync<RenameParams, WorkspaceEdit>(Methods.TextDocumentRenameName, renameParams, _clientCapabilities, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentImplementationName, UseSingleObjectParameterDeserialization = true)]
        public Task<Location[]> GoToImplementationAsync(TextDocumentPositionParams positionParams, CancellationToken cancellationToken)
        {
            if (positionParams is null)
            {
                throw new ArgumentNullException(nameof(positionParams));
            }

            return ExecuteRequestAsync<TextDocumentPositionParams, Location[]>(Methods.TextDocumentImplementationName, positionParams, _clientCapabilities, cancellationToken);
        }

        // Internal for testing
        internal Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(
            string methodName,
            RequestType request,
            ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken) where RequestType : class
        {
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException("Invalid method name", nameof(methodName));
            }

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var handler = (IRequestHandler<RequestType, ResponseType>)_requestHandlers[methodName]?.Value;

            if (handler is null)
            {
                throw new InvalidOperationException($"Request handler not found for method {methodName}");
            }

            return handler.HandleRequestAsync(request, clientCapabilities, cancellationToken);
        }

        private static ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> CreateMethodToHandlerMap(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<string, Lazy<IRequestHandler, IRequestHandlerMetadata>>();
            foreach (var lazyHandler in requestHandlers)
            {
                requestHandlerDictionary.Add(lazyHandler.Metadata.MethodName, lazyHandler);
            }

            return requestHandlerDictionary.ToImmutable();
        }

        public void Dispose()
        {
            try
            {
                if (!_jsonRpc.IsDisposed)
                {
                    _jsonRpc.Dispose();
                }
            }
            catch (Exception)
            {
                // Swallow exceptions thrown by disposing our JsonRpc object. Disconnected events can potentially throw their own exceptions so
                // we purposefully ignore all of those exceptions in an effort to shutdown gracefully.
            }
        }
    }
}
