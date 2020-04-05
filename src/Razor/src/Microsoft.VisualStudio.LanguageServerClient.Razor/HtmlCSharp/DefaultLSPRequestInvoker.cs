// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Shared]
    [Export(typeof(LSPRequestInvoker))]
    internal class DefaultLSPRequestInvoker : LSPRequestInvoker
    {
        private readonly ILanguageClientBroker _languageClientBroker;
        private readonly MethodInfo _requestAsyncMethod;

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
        }

        public async override Task<TOut> RequestServerAsync<TIn, TOut>(string method, LanguageServerKind serverKind, TIn parameters, CancellationToken cancellationToken)
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
            var task = (Task<(ILanguageClient, JToken)>)_requestAsyncMethod.Invoke(
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

            var result = resultToken != null ? resultToken.ToObject<TOut>() : default;
            return result;
        }
    }
}
