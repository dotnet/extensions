// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Newtonsoft.Json.Linq;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception
{
    /// <summary>
    /// Receives notification messages from the server and invokes any applicable message interception layers.
    /// </summary>
    public class InterceptionMiddleLayer : ILanguageClientMiddleLayer
    {
        private readonly InterceptorManager _interceptorManager;
        private readonly string _languageName;

        /// <summary>
        /// Create the middle layer
        /// </summary>
        /// <param name="interceptorManager">Interception manager</param>
        /// <param name="languageName">The content type name of the language for the ILanguageClient using this middle layer</param>
        public InterceptionMiddleLayer(InterceptorManager interceptorManager, string languageName)
        {
            _interceptorManager = interceptorManager ?? throw new ArgumentNullException(nameof(interceptorManager));
            _languageName = !string.IsNullOrEmpty(languageName) ? languageName : throw new ArgumentException("Cannot be empty", nameof(languageName));
        }

        public bool CanHandle(string methodName)
        {
            return _interceptorManager.HasInterceptor(methodName);
        }

        public async Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
        {
            var payload = methodParam;
            if (CanHandle(methodName))
            {
                payload = await _interceptorManager.ProcessInterceptorsAsync(methodName, methodParam, _languageName, CancellationToken.None);
            }

            if (!(payload is null))
            {
                // this completes the handshake to give the payload back to the client.
                await sendNotification(payload);
            }
        }

        public Task<JToken?> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken?>> sendRequest)
        {
            // until we have a request operation that needs to support interception, just pass the request through
            return sendRequest(methodParam);
        }
    }
}
