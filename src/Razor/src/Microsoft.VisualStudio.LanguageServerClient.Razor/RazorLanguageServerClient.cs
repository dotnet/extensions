// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Nerdbank.Streams;
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
        private readonly FeedbackFileLoggerProviderFactory _feedbackFileLoggerProviderFactory;
        private object _shutdownLock;
        private RazorLanguageServer _server;
        private IDisposable _serverShutdownDisposable;

        private const string RazorLSPLogLevel = "RAZOR_TRACE";

        [ImportingConstructor]
        public RazorLanguageServerClient(
            RazorLanguageServerCustomMessageTarget customTarget,
            RazorLanguageClientMiddleLayer middleLayer,
            LSPRequestInvoker requestInvoker,
            ProjectConfigurationFilePathStore projectConfigurationFilePathStore,
            FeedbackFileLoggerProviderFactory feedbackFileLoggerProviderFactory)
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

            if (feedbackFileLoggerProviderFactory is null)
            {
                throw new ArgumentNullException(nameof(feedbackFileLoggerProviderFactory));
            }

            _customMessageTarget = customTarget;
            _middleLayer = middleLayer;
            _requestInvoker = requestInvoker;
            _projectConfigurationFilePathStore = projectConfigurationFilePathStore;
            _feedbackFileLoggerProviderFactory = feedbackFileLoggerProviderFactory;
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

            await EnsureCleanedUpServerAsync(token).ConfigureAwait(false);

            // Need an auto-flushing stream for the server because O# doesn't currently flush after writing responses. Without this
            // performing the Initialize handshake with the LanguageServer hangs.
            var autoFlushingStream = new AutoFlushingNerdbankStream(serverStream);
            var traceLevel = GetVerbosity();
            _server = await RazorLanguageServer.CreateAsync(autoFlushingStream, autoFlushingStream, traceLevel, ConfigureLanguageServer).ConfigureAwait(false);

            // Fire and forget for Initialized. Need to allow the LSP infrastructure to run in order to actually Initialize.
            _server.InitializedAsync(token).FileAndForget("RazorLanguageServerClient_ActivateAsync");

            var connection = new Connection(clientStream, clientStream);
            return connection;
        }

        private void ConfigureLanguageServer(IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var loggerProvider = _feedbackFileLoggerProviderFactory.GetOrCreate();
            services.AddSingleton<ILoggerProvider>(loggerProvider);
        }

        private Trace GetVerbosity()
        {
            Trace result;

            // Since you can't set an Environment variable in CodeSpaces we need to default that scenario to Verbose.
            if (IsVSServer())
            {
                result = Trace.Verbose;
            }
            else
            {
                var logString = Environment.GetEnvironmentVariable(RazorLSPLogLevel);
                if (Enum.TryParse<Trace>(logString, out var parsedTrace))
                {
                    result = parsedTrace;
                }
                else
                {
                    result = Trace.Off;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true if the client is a CodeSpace instance.
        /// </summary>
        protected virtual bool IsVSServer()
        {
            var shell = AsyncPackage.GetGlobalService(typeof(SVsShell)) as IVsShell;
            var result = shell.GetProperty((int)__VSSPROPID11.VSSPROPID_ShellMode, out var mode);

            bool isVSServer;
            if (ErrorHandler.Succeeded(result))
            {
                isVSServer = ((int)mode == (int)__VSShellMode.VSSM_Server);
            }
            else
            {
                isVSServer = false;
            }

            return isVSServer;
        }

        private async Task EnsureCleanedUpServerAsync(CancellationToken token)
        {
            const int WaitForShutdownAttempts = 10;

            if (_server == null)
            {
                // Server was already cleaned up
                return;
            }

            var attempts = 0;
            while (_server != null && ++attempts < WaitForShutdownAttempts)
            {
                // Server failed to shutdown, lets wait a little bit and check again.
                await Task.Delay(100, token);
            }

            lock (_shutdownLock)
            {
                if (_server != null)
                {
                    // Server still hasn't shutdown, attempt an ungraceful shutdown.
                    _server.Dispose();

                    ServerShutdown();
                }
            }
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
            _serverShutdownDisposable = _server.OnShutdown.Subscribe((_) => ServerShutdown());

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

#pragma warning disable VSTHRD100 // Avoid async void methods
        private async void ProjectConfigurationFilePathStore_Changed(object sender, ProjectConfigurationFilePathChangedEventArgs args)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            try
            {
                var parameter = new MonitorProjectConfigurationFilePathParams()
                {
                    ProjectFilePath = args.ProjectFilePath,
                    ConfigurationFilePath = args.ConfigurationFilePath,
                };

                await _requestInvoker.ReinvokeRequestOnServerAsync<MonitorProjectConfigurationFilePathParams, object>(
                    LanguageServerConstants.RazorMonitorProjectConfigurationFilePathEndpoint,
                    LanguageServerKind.Razor,
                    parameter,
                    CancellationToken.None);
            }
            catch (Exception)
            {
                // We're fire and forgetting here, if the request fails we're ok with that.
                //
                // Note: When moving between solutions this can fail with a null reference exception because the underlying LSP platform's
                // JsonRpc object will be `null`. This can happen in two situations:
                //      1.  There's currently a race in the platform on shutting down/activating so we don't get the opportunity to properly detatch
                //          from the configuration file path store changed event properly.
                //          Tracked by: https://github.com/dotnet/aspnetcore/issues/23819
                //      2.  The LSP platform failed to shutdown our language server properly due to a JsonRpc timeout. There's currently a limitation in
                //          the LSP platform APIs where we don't know if the LSP platform requested shutdown but our language server never saw it. Therefore,
                //          we will null-ref until our language server client boot-logic kicks back in and re-activates resulting in the old server being
                //          being cleaned up.
            }
        }

        public Task AttachForCustomMessageAsync(JsonRpc rpc) => Task.CompletedTask;
    }
}
