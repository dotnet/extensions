// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Nerdbank.Streams;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    [ClientName(ClientName)]
    [Export(typeof(ILanguageClient))]
    [ContentType(RazorLSPContentTypeDefinition.Name)]
    internal class RazorHtmlCSharpLanguageServerClient : ILanguageClient
    {
        // ClientName enables us to turn on-off the ILanguageClient functionality for specific TextBuffers of content type RazorLSPContentTypeDefinition.Name.
        // This typically is used in cloud scenarios where we want to utilize an ILanguageClient on the server but not the client; therefore we disable this
        // ILanguageClient infrastructure on the guest to ensure that two language servers don't provide results.
        public const string ClientName = "RazorLSPClientName";
        private readonly IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> _requestHandlers;

        [ImportingConstructor]
        public RazorHtmlCSharpLanguageServerClient([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
        {
            if (requestHandlers is null)
            {
                throw new ArgumentNullException(nameof(requestHandlers));
            }

            _requestHandlers = requestHandlers;
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

        public Task<Connection> ActivateAsync(CancellationToken token)
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();

            _ = new RazorHtmlCSharpLanguageServer(serverStream, serverStream, _requestHandlers);

            var connection = new Connection(clientStream, clientStream);
            return Task.FromResult(connection);
        }

        public async Task OnLoadedAsync()
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializedAsync()
        {
            return Task.CompletedTask;
        }
    }
}
