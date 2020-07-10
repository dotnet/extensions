// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Nerdbank.Streams;
using OmniSharp.Extensions.LanguageServer.Server;
using StreamJsonRpc;
using Task = System.Threading.Tasks.Task;
using Trace = Microsoft.AspNetCore.Razor.LanguageServer.Trace;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    [Export(typeof(ILanguageClient))]
    [ContentType(RazorLSPConstants.RazorLSPContentTypeName)]
    internal class RazorLanguageServerClient : ILanguageClient, ILanguageClientCustomMessage2, ILanguageClientPriority
    {
        private readonly RazorLanguageServerCustomMessageTarget _customMessageTarget;
        private readonly ILanguageClientMiddleLayer _middleLayer;
        private readonly LSPRequestInvoker _requestInvoker;
        private readonly ProjectConfigurationFilePathStore _projectConfigurationFilePathStore;
        private object _shutdownLock;
        private ILanguageServer _server;
        private IDisposable _serverShutdownDisposable;

        [ImportingConstructor]
        public RazorLanguageServerClient(
            RazorLanguageServerCustomMessageTarget customTarget,
            RazorLanguageClientMiddleLayer middleLayer,
            LSPRequestInvoker requestInvoker,
            ProjectConfigurationFilePathStore projectConfigurationFilePathStore)
        {
            if (customTarget is null)
            {
                throw new ArgumentNullException(nameof(customTarget));
            }

            if (middleLayer is null)
            {
                throw new ArgumentNullException(nameof(middleLayer));
            }

            if (requestInvoker is null)
            {
                throw new ArgumentNullException(nameof(requestInvoker));
            }

            if (projectConfigurationFilePathStore is null)
            {
                throw new ArgumentNullException(nameof(projectConfigurationFilePathStore));
            }

            _customMessageTarget = customTarget;
            _middleLayer = middleLayer;
            _requestInvoker = requestInvoker;
            _projectConfigurationFilePathStore = projectConfigurationFilePathStore;
            _shutdownLock = new object();
        }

        public string Name => "Razor Language Server Client";

        public IEnumerable<string> ConfigurationSections => null;

        public object InitializationOptions => null;

        public IEnumerable<string> FilesToWatch => null;

        public object MiddleLayer => _middleLayer;

        public object CustomMessageTarget => _customMessageTarget;

        public bool IsOverriding => false;

        // We set a priority to ensure that our Razor language server is always chosen if there's a conflict for which language server to prefer.
        public int Priority => 10;

        public event AsyncEventHandler<EventArgs> StartAsync;
        public event AsyncEventHandler<EventArgs> StopAsync
        {
            add { }
            remove { }
        }

        public async Task<Connection> ActivateAsync(CancellationToken token)
        {
            var (clientStream, serverStream) = FullDuplexStream.CreatePair();
            // Need an auto-flushing stream for the server because O# doesn't currently flush after writing responses. Without this
            // performing the Initialize handshake with the LanguageServer hangs.
            var autoFlushingStream = new AutoFlushingNerdbankStream(serverStream);
            _server = await RazorLanguageServer.CreateAsync(autoFlushingStream, autoFlushingStream, Trace.Verbose).ConfigureAwait(false);

            // Fire and forget for Initialized. Need to allow the LSP infrastructure to run in order to actually Initialize.
            _server.InitializedAsync(token).FileAndForget("RazorLanguageServerClient_ActivateAsync");

            var connection = new Connection(clientStream, clientStream);
            return connection;
        }

        public async Task OnLoadedAsync()
        {
            await StartAsync.InvokeAsync(this, EventArgs.Empty).ConfigureAwait(false);
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            return Task.CompletedTask;
        }

        public Task OnServerInitializedAsync()
        {
            _serverShutdownDisposable = _server.Shutdown.Subscribe((_) => ServerShutdown());

            ServerStarted();

            return Task.CompletedTask;
        }

        private void ServerStarted()
        {
            _projectConfigurationFilePathStore.Changed += ProjectConfigurationFilePathStore_Changed;

            var mappings = _projectConfigurationFilePathStore.GetMappings();
            foreach (var mapping in mappings)
            {
                var args = new ProjectConfigurationFilePathChangedEventArgs(mapping.Key, mapping.Value);
                ProjectConfigurationFilePathStore_Changed(this, args);
            }
        }

        private void ServerShutdown()
        {
            lock (_shutdownLock)
            {
                if (_server == null)
                {
                    // Already shutdown
                    return;
                }

                _projectConfigurationFilePathStore.Changed -= ProjectConfigurationFilePathStore_Changed;
                _serverShutdownDisposable?.Dispose();
                _serverShutdownDisposable = null;
                _server = null;
            }
        }

        private async void ProjectConfigurationFilePathStore_Changed(object sender, ProjectConfigurationFilePathChangedEventArgs args)
        {
            try
            {
                var parameter = new MonitorProjectConfigurationFilePathParams()
                {
                    ProjectFilePath = args.ProjectFilePath,
                    ConfigurationFilePath = args.ConfigurationFilePath,
                };

                await _requestInvoker.CustomRequestServerAsync<MonitorProjectConfigurationFilePathParams, object>(
                    LanguageServerConstants.RazorMonitorProjectConfigurationFilePathEndpoint,
                    LanguageServerKind.Razor,
                    parameter,
                    CancellationToken.None);
            }
            catch (Exception)
            {
                // We're fire and forgetting here, if the request fails we're ok with that.
            }
        }

        public Task AttachForCustomMessageAsync(JsonRpc rpc) => Task.CompletedTask;
    }
}
