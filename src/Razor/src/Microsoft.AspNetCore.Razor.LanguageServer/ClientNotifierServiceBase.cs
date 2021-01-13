// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    internal abstract class ClientNotifierServiceBase: IOnLanguageServerStarted
    {
        public abstract Task<IResponseRouterReturns> SendRequestAsync(string method);

        public abstract Task<IResponseRouterReturns> SendRequestAsync<T>(string method, T @params);

        public abstract Task OnStarted(ILanguageServer server, CancellationToken cancellationToken);

        public abstract InitializeParams ClientSettings { get; }
    }
}
