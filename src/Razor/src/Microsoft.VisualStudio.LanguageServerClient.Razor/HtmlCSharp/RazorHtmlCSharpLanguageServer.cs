// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    internal class RazorHtmlCSharpLanguageServer : IDisposable
    {
        private readonly JsonRpc _jsonRpc;
        private readonly ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> _requestHandlers;
        private VSClientCapabilities _clientCapabilities;

        private RazorHtmlCSharpLanguageServer(
            Stream inputStream,
            Stream outputStream,
            IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers,
            HTMLCSharpLanguageServerLogHubLoggerProvider loggerProvider) : this(requestHandlers)
        {
            _jsonRpc = CreateJsonRpc(outputStream, inputStream, target: this);

            // Facilitates activity based tracing for structured logging within LogHub
            var traceSource = loggerProvider.GetTraceSource();
            _jsonRpc.ActivityTracingStrategy = new CorrelationManagerTracingStrategy
            {
                TraceSource = traceSource
            };
            _jsonRpc.TraceSource = traceSource;

            _jsonRpc.StartListening();
        }

        public static async Task<RazorHtmlCSharpLanguageServer> CreateAsync(
            Stream inputStream,
            Stream outputStream,
            IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers,
            HTMLCSharpLanguageServerLogHubLoggerProvider loggerProvider,
            CancellationToken cancellationToken)
        {
            if (inputStream is null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }

            if (outputStream is null)
            {
                throw new ArgumentNullException(nameof(outputStream));
            }

            if (loggerProvider is null)
            {
                throw new ArgumentNullException(nameof(loggerProvider));
            }

            // Wait for logging infrastructure to initialize. This must be completed
            // before we can start listening via Json RPC (as we must link the log hub
            // trace source with Json RPC to facilitate structured logging / activity tracing).
            await loggerProvider.InitializeLoggerAsync(cancellationToken).ConfigureAwait(false);

            return new RazorHtmlCSharpLanguageServer(inputStream, outputStream, requestHandlers, loggerProvider);
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

        [JsonRpcMethod(MSLSPMethods.OnTypeRenameName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentOnTypeRenameResponseItem> OnTypeRenameAsync(DocumentOnTypeRenameParams request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return ExecuteRequestAsync<DocumentOnTypeRenameParams, DocumentOnTypeRenameResponseItem>(MSLSPMethods.OnTypeRenameName, request, _clientCapabilities, cancellationToken);
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

        [JsonRpcMethod(MSLSPMethods.DocumentPullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<DiagnosticReport[]> DocumentPullDiagnosticsAsync(DocumentDiagnosticsParams documentDiagnosticsParams, CancellationToken cancellationToken)
        {
            if (documentDiagnosticsParams is null)
            {
                throw new ArgumentNullException(nameof(documentDiagnosticsParams));
            }

            return ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]>(MSLSPMethods.DocumentPullDiagnosticName, documentDiagnosticsParams, _clientCapabilities, cancellationToken);
        }

        // Razor tooling doesn't utilize workspace pull diagnostics as it doesn't really make sense for our use case. 
        // However, without the workspace pull diagnostics endpoint, a bunch of unnecessary exceptions are
        // triggered. Thus we add the following no-op handler until a server capability is available.
        // Having a server capability would reduce overhead of sending/receiving the request and the
        // associated serialization/deserialization.
        [JsonRpcMethod(MSLSPMethods.WorkspacePullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public static Task<WorkspaceDiagnosticReport> WorkspacePullDiagnosticsAsync(WorkspaceDocumentDiagnosticsParams workspaceDiagnosticsParams, CancellationToken cancellationToken)
        {
            return Task.FromResult<WorkspaceDiagnosticReport>(null);
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

        private static JsonRpc CreateJsonRpc(Stream outputStream, Stream inputStream, object target)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var messageFormatter = new JsonMessageFormatter();
#pragma warning restore CA2000 // Dispose objects before losing scope

            var serializer = messageFormatter.JsonSerializer;
            AddVSExtensionConverters(serializer);

#pragma warning disable CA2000 // Dispose objects before losing scope
            var messageHandler = new HeaderDelimitedMessageHandler(outputStream, inputStream, messageFormatter);
#pragma warning restore CA2000 // Dispose objects before losing scope

            // The JsonRpc object owns disposing the message handler which disposes the formatter.
            var jsonRpc = new JsonRpc(messageHandler, target);
            return jsonRpc;

            // Can be removed for serializer.AddVSExtensionConverters() once we are able to update to a newer LSP protocol Extensions version.
            void AddVSExtensionConverters(JsonSerializer serializer)
            {
                serializer.Converters.Add(new VSExtensionConverter<ClientCapabilities, VSClientCapabilities>());
                serializer.Converters.Add(new VSExtensionConverter<CodeAction, VSCodeAction>());
                serializer.Converters.Add(new VSExtensionConverter<CodeActionContext, VSCodeActionContext>());
                serializer.Converters.Add(new VSExtensionConverter<CompletionContext, VSCompletionContext>());
                serializer.Converters.Add(new VSExtensionConverter<CompletionItem, VSCompletionItem>());
                serializer.Converters.Add(new VSExtensionConverter<CompletionList, VSCompletionList>());
                serializer.Converters.Add(new VSExtensionConverter<Diagnostic, VSDiagnostic>());
                serializer.Converters.Add(new VSExtensionConverter<Hover, VSHover>());
                serializer.Converters.Add(new VSExtensionConverter<ServerCapabilities, VSServerCapabilities>());
                serializer.Converters.Add(new VSExtensionConverter<SignatureInformation, VSSignatureInformation>());
                serializer.Converters.Add(new VSExtensionConverter<SymbolInformation, VSSymbolInformation>());
                serializer.Converters.Add(new VSExtensionConverter<TextDocumentClientCapabilities, VSTextDocumentClientCapabilities>());
                serializer.Converters.Add(new VSExtensionConverter<TextDocumentIdentifier, VSTextDocumentIdentifier>());
            }
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
