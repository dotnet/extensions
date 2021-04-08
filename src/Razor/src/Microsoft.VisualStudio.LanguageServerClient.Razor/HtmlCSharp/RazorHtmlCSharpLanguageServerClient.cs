// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Logging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Nerdbank.Streams;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [Export(typeof(ILanguageClient))]
    [ContentType(RazorLSPConstants.RazorLSPContentTypeName)]
    internal class RazorHtmlCSharpLanguageServerClient : ILanguageClient, IDisposable
    {
        private readonly IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> _requestHandlers;
        private readonly HTMLCSharpLanguageServerLogHubLoggerProvider _loggerProvider;
        private RazorHtmlCSharpLanguageServer _languageServer;

        [ImportingConstructor]
        public RazorHtmlCSharpLanguageServerClient(
            [ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers,
            HTMLCSharpLanguageServerLogHubLoggerProvider loggerProvider)
        {
            if (requestHandlers is null)
            {
                throw new ArgumentNullException(nameof(requestHandlers));
            }

            if (loggerProvider is null)
            {
                throw new ArgumentNullException(nameof(loggerProvider));
            }

            _requestHandlers = requestHandlers;
            _loggerProvider = loggerProvider;
        }

        public string Name => "Razor Html & CSharp Language Server Client";

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public event AsyncEventHandler<EventArgs> StartAsync;

        public event AsyncEventHandler<EventArgs> StopAsync
        {
            add { }
            remove { }
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();

            _languageServer = await RazorHtmlCSharpLanguageServer.CreateAsync(serverStream, serverStream, _requestHandlers, _loggerProvider, token);

            var connection = new Connection(clientStream, clientStream);
            return connection;
        }

        public Task OnLoadedAsync()
        {
            return StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _languageServer?.Dispose();
        }
    }
}
