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
    internal class RazorHtmlCSharpLanguageServer
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
    }
}
