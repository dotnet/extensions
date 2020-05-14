// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPRequestInvoker))]
    internal class DefaultLSPRequestInvoker : LSPRequestInvoker
    {
        private readonly ILanguageClientBroker _languageClientBroker;
        private readonly MethodInfo _synchronizedRequestAsyncMethod;
        private readonly MethodInfo _requestAsyncMethod;
        private readonly JsonSerializer _serializer;

        [ImportingConstructor]
        public DefaultLSPRequestInvoker(ILanguageClientBroker languageClientBroker)
        {
            if (languageClientBroker is null)
            {
                throw new ArgumentNullException(nameof(languageClientBroker));
            }

            _languageClientBroker = languageClientBroker;

            // Ideally we want to call ILanguageServiceBroker2.RequestAsync directly but it is not referenced
            // because the LanguageClient.Implementation assembly isn't published to a public feed.
            // So for now, we invoke it using reflection. This will go away eventually.
            var type = _languageClientBroker.GetType();
            _requestAsyncMethod = type.GetMethod(
                "RequestAsync",
                new[]
                {
                    typeof(string[]),
                    typeof(Func<JToken, bool>),
                    typeof(string),
                    typeof(JToken),
                    typeof(CancellationToken)
                });
            _synchronizedRequestAsyncMethod = type.GetMethod(
                "SynchronizedRequestAsync",
                new[]
                {
                    typeof(string[]),
                    typeof(Func<JToken, bool>),
                    typeof(string),
                    typeof(JToken),
                    typeof(CancellationToken)
                });

            // We need these converters so we don't lose information as part of the deserialization.
            _serializer = new JsonSerializer();
            _serializer.Converters.Add(new VSExtensionConverter<ClientCapabilities, VSClientCapabilities>());
            _serializer.Converters.Add(new VSExtensionConverter<CompletionItem, VSCompletionItem>());
            _serializer.Converters.Add(new VSExtensionConverter<SignatureInformation, VSSignatureInformation>());
            _serializer.Converters.Add(new VSExtensionConverter<Hover, VSHover>());
            _serializer.Converters.Add(new VSExtensionConverter<ServerCapabilities, VSServerCapabilities>());
            _serializer.Converters.Add(new VSExtensionConverter<SymbolInformation, VSSymbolInformation>());
            _serializer.Converters.Add(new VSExtensionConverter<CompletionList, VSCompletionList>());
        }

        public override Task<TOut> CustomRequestServerAsync<TIn, TOut>(string method, LanguageServerKind serverKind, TIn parameters, CancellationToken cancellationToken)
        {
            return RequestServerCoreAsync<TIn, TOut>(_requestAsyncMethod, method, serverKind, parameters, cancellationToken);
        }

        public override Task<TOut> ReinvokeRequestOnServerAsync<TIn, TOut>(string method, LanguageServerKind serverKind, TIn parameters, CancellationToken cancellationToken)
        {
            return RequestServerCoreAsync<TIn, TOut>(_synchronizedRequestAsyncMethod, method, serverKind, parameters, cancellationToken);
        }

        private async Task<TOut> RequestServerCoreAsync<TIn, TOut>(MethodInfo lspPlatformMethod, string method, LanguageServerKind serverKind, TIn parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("message", nameof(method));
            }

            var contentType = RazorLSPContentTypeDefinition.Name;
            if (serverKind == LanguageServerKind.CSharp)
            {
                contentType = CSharpVirtualDocumentFactory.CSharpLSPContentTypeName;
            }
            else if (serverKind == LanguageServerKind.Html)
            {
                contentType = HtmlVirtualDocumentFactory.HtmlLSPContentTypeName;
            }

            var serializedParams = JToken.FromObject(parameters);
            var task = (Task<(ILanguageClient, JToken)>)lspPlatformMethod.Invoke(
                _languageClientBroker,
                new object[]
                {
                    new[] { contentType },
                    (Func<JToken, bool>)(token => true),
                    method,
                    serializedParams,
                    cancellationToken
                });

            var (_, resultToken) = await task.ConfigureAwait(false);

            var result = resultToken != null ? resultToken.ToObject<TOut>(_serializer) : default;
            return result;
        }
    }
}
