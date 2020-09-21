// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage
{
    [Shared]
    [Export(typeof(LSPRequestInvoker))]
    internal class DefaultLSPRequestInvoker : LSPRequestInvoker
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly JsonSerializer _serializer;

        [ImportingConstructor]
        public DefaultLSPRequestInvoker(ILanguageServiceBroker2 languageServiceBroker)
        {
            if (languageServiceBroker is null)
            {
                throw new ArgumentNullException(nameof(languageServiceBroker));
            }

            _languageServiceBroker = languageServiceBroker;

            // We need these converters so we don't lose information as part of the deserialization.
            _serializer = new JsonSerializer();
            _serializer.Converters.Add(new VSExtensionConverter<ClientCapabilities, VSClientCapabilities>());
            _serializer.Converters.Add(new VSExtensionConverter<CompletionItem, VSCompletionItem>());
            _serializer.Converters.Add(new VSExtensionConverter<SignatureInformation, VSSignatureInformation>());
            _serializer.Converters.Add(new VSExtensionConverter<Hover, VSHover>());
            _serializer.Converters.Add(new VSExtensionConverter<ServerCapabilities, VSServerCapabilities>());
            _serializer.Converters.Add(new VSExtensionConverter<SymbolInformation, VSSymbolInformation>());
            _serializer.Converters.Add(new VSExtensionConverter<CompletionList, VSCompletionList>());
            _serializer.Converters.Add(new VSExtensionConverter<CodeAction, VSCodeAction>());
        }

        public override Task<TOut> ReinvokeRequestOnServerAsync<TIn, TOut>(string method, string contentType, TIn parameters, CancellationToken cancellationToken)
        {
            return RequestServerCoreAsync<TIn, TOut>(method, contentType, token => true, parameters, cancellationToken);
        }

        public override Task<TOut> ReinvokeRequestOnServerAsync<TIn, TOut>(string method, string contentType, Func<JToken, bool> capabilitiesFilter, TIn parameters, CancellationToken cancellationToken)
        {
            return RequestServerCoreAsync<TIn, TOut>(method, contentType, capabilitiesFilter, parameters, cancellationToken);
        }

        public override Task<IEnumerable<TOut>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(string method, string contentType, TIn parameters, CancellationToken cancellationToken)
        {
            return RequestMultipleServerCoreAsync<TIn, TOut>(method, contentType, token => true, parameters, cancellationToken);
        }

        public override Task<IEnumerable<TOut>> ReinvokeRequestOnMultipleServersAsync<TIn, TOut>(string method, string contentType, Func<JToken, bool> capabilitiesFilter, TIn parameters, CancellationToken cancellationToken)
        {
            return RequestMultipleServerCoreAsync<TIn, TOut>(method, contentType, capabilitiesFilter, parameters, cancellationToken);
        }

        private async Task<TOut> RequestServerCoreAsync<TIn, TOut>(string method, string contentType, Func<JToken, bool> capabilitiesFilter, TIn parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("message", nameof(method));
            }

            var serializedParams = JToken.FromObject(parameters);

            var (_, resultToken) = await _languageServiceBroker.RequestAsync(
                new[] { contentType },
                capabilitiesFilter,
                method,
                serializedParams,
                cancellationToken).ConfigureAwait(false);

            var result = resultToken != null ? resultToken.ToObject<TOut>(_serializer) : default;
            return result;
        }

        private async Task<IEnumerable<TOut>> RequestMultipleServerCoreAsync<TIn, TOut>(string method, string contentType, Func<JToken, bool> capabilitiesFilter, TIn parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException("message", nameof(method));
            }

            var serializedParams = JToken.FromObject(parameters);

            var clientAndResultTokenPairs = await _languageServiceBroker.RequestMultipleAsync(
                new[] { contentType },
                capabilitiesFilter,
                method,
                serializedParams,
                cancellationToken).ConfigureAwait(false);

            // a little ugly - tuple deconstruction in lambda arguments doesn't work - https://github.com/dotnet/csharplang/issues/258
            var results = clientAndResultTokenPairs.Select((clientAndResultToken) => clientAndResultToken.Item2 != null ? clientAndResultToken.Item2.ToObject<TOut>(_serializer) : default);

            return results;
        }
    }
}
