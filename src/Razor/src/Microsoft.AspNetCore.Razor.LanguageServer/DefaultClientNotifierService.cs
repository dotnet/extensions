// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    // The VSCode OmniSharp client starts the RazorServer before all of its handlers are registered
    // because of this we need to wait until everthing is initialized to make some client requests like
    // razor\serverReady. This class takes a TCS which will complete when everything is initialized
    // ensuring that no requests are sent before the client is ready.
    internal class DefaultClientNotifierService : ClientNotifierServiceBase
    {
        private readonly TaskCompletionSource<bool> _initializedCompletionSource;
        private readonly IClientLanguageServer _languageServer;

        public DefaultClientNotifierService(IClientLanguageServer languageServer)
        {
            if (languageServer is null)
            {
                throw new ArgumentNullException(nameof(languageServer));
            }

            _languageServer = languageServer;
            _initializedCompletionSource = new TaskCompletionSource<bool>();
        }

        public override async Task<IResponseRouterReturns> SendRequestAsync(string method)
        {
            await _initializedCompletionSource.Task;

            return _languageServer.SendRequest(method);
        }

        public override async Task<IResponseRouterReturns> SendRequestAsync<T>(string method, T @params)
        {
            await _initializedCompletionSource.Task;

            return _languageServer.SendRequest(method, @params);
        }

        /// <summary>
        /// Fires when the language server is set to "Started".
        /// </summary>
        /// <param name="server"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task OnStarted(ILanguageServer server, CancellationToken cancellationToken)
        {
            _initializedCompletionSource.TrySetResult(true);
            return Task.CompletedTask;
        }

        public override InitializeParams ClientSettings => _languageServer.ClientSettings;
    }
}
