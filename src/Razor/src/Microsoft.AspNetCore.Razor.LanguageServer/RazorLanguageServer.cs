// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.LanguageServer.Hover;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.AspNetCore.Razor.LanguageServer.CodeActions;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using System.Threading;
using Microsoft.AspNetCore.Razor.LanguageServer.Refactoring;
using Microsoft.AspNetCore.Razor.LanguageServer.Definition;
using Microsoft.AspNetCore.Razor.LanguageServer.Serialization;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public sealed class RazorLanguageServer : IDisposable
    {
        private readonly ILanguageServer _innerServer;
        private readonly object _disposeLock;
        private bool _disposed;

        private RazorLanguageServer(ILanguageServer innerServer)
        {
            if (innerServer is null)
            {
                throw new ArgumentNullException(nameof(innerServer));
            }

            _innerServer = innerServer;
            _disposeLock = new object();
        }

        public IObservable<bool> OnShutdown => _innerServer.Shutdown;

        public Task WaitForExit => _innerServer.WaitForExit;

        public Task InitializedAsync(CancellationToken token) => _innerServer.Initialize(token);

        public static Task<RazorLanguageServer> CreateAsync(Stream input, Stream output, Trace trace, Action<RazorLanguageServerBuilder> configure = null)
        {
            Serializer.Instance.JsonSerializer.Converters.RegisterRazorConverters();

            // Custom ClientCapabilities deserializer to extract experimental capabilities
            Serializer.Instance.JsonSerializer.Converters.Add(ExtendableClientCapabilitiesJsonConverter.Instance);

            ILanguageServer server = null;
            var logLevel = RazorLSPOptions.GetLogLevelForTrace(trace);

            server = OmniSharp.Extensions.LanguageServer.Server.LanguageServer.PreInit(options =>
                options
                    .WithInput(input)
                    .WithOutput(output)
                    // StreamJsonRpc has both Serial and Parallel requests. With WithContentModifiedSupport(true) (which is default) when a Serial
                    // request is made any Parallel requests will be cancelled because the assumption is that Serial requests modify state, and that
                    // therefore any Parallel request is now invalid and should just try again. A specific instance of this can be seen when you
                    // hover over a TagHelper while the switch is set to true. Hover is parallel, and a lot of our endpoints like
                    // textDocument/_ms_onAutoInsert, and razor/languageQuery are Serial. I BELIEVE that specifically what happened is the serial
                    // languageQuery event gets fired by our semantic tokens endpoint (which fires constantly), cancelling the hover, which red-bars.
                    // We can prevent that behavior entirely by doing WithContentModifiedSupport, at the possible expense of some delays due doing all requests in serial.
                    //
                    // I recommend that we attempt to resolve this and switch back to WithContentModifiedSupport(true) in the future,
                    // I think that would mean either having 0 Serial Handlers in the whole LS, or making VSLanguageServerClient handle this more gracefully.
                    .WithContentModifiedSupport(false)
                    .WithSerializer(Serializer.Instance)
                    .ConfigureLogging(builder => builder
                        .SetMinimumLevel(logLevel)
                        .AddLanguageProtocolLogging(logLevel))
                    .OnInitialized(async (s, request, response, cancellationToken) =>
                    {
                        var handlersManager = s.GetRequiredService<IHandlersManager>();
                        var jsonRpcHandlers = handlersManager.Descriptors.Select(d => d.Handler);
                        var registrationExtensions = jsonRpcHandlers.OfType<IRegistrationExtension>().Distinct();
                        if (registrationExtensions.Any())
                        {
                            var capabilities = new ExtendableServerCapabilities(response.Capabilities, registrationExtensions);
                            response.Capabilities = capabilities;
                        }
                        var fileChangeDetectorManager = s.Services.GetRequiredService<RazorFileChangeDetectorManager>();
                        await fileChangeDetectorManager.InitializedAsync();

                        // Workaround for https://github.com/OmniSharp/csharp-language-server-protocol/issues/106
                        var languageServer = (OmniSharp.Extensions.LanguageServer.Server.LanguageServer)server;
                        if (request.Capabilities.Workspace.Configuration.IsSupported)
                        {
                            // Initialize our options for the first time.
                            var optionsMonitor = languageServer.Services.GetRequiredService<RazorLSPOptionsMonitor>();
                            _ = Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(async (_) => await optionsMonitor.UpdateAsync(cancellationToken));
                        }
                    })
                    .WithHandler<RazorDocumentSynchronizationEndpoint>()
                    .WithHandler<RazorCompletionEndpoint>()
                    .WithHandler<RazorHoverEndpoint>()
                    .WithHandler<RazorLanguageEndpoint>()
                    .WithHandler<RazorConfigurationEndpoint>()
                    .WithHandler<RazorFormattingEndpoint>()
                    .WithHandler<RazorSemanticTokensEndpoint>()
                    .AddHandlerLink(LanguageServerConstants.RazorSemanticTokensEditEndpoint, LanguageServerConstants.LegacyRazorSemanticTokensEditEndpoint)
                    .AddHandlerLink(LanguageServerConstants.RazorSemanticTokensEndpoint , LanguageServerConstants.LegacyRazorSemanticTokensEndpoint)
                    .WithHandler<RazorSemanticTokensLegendEndpoint>()
                    .WithHandler<OnAutoInsertEndpoint>()
                    .WithHandler<CodeActionEndpoint>()
                    .WithHandler<CodeActionResolutionEndpoint>()
                    .WithHandler<MonitorProjectConfigurationFilePathEndpoint>()
                    .WithHandler<RazorComponentRenameEndpoint>()
                    .WithHandler<RazorDefinitionEndpoint>()
                    .WithServices(services =>
                    {
                        if (configure != null)
                        {
                            var builder = new RazorLanguageServerBuilder(services);
                            configure(builder);
                        }

                        var filePathNormalizer = new FilePathNormalizer();
                        services.AddSingleton<FilePathNormalizer>(filePathNormalizer);

                        var foregroundDispatcher = new DefaultForegroundDispatcher();
                        services.AddSingleton<ForegroundDispatcher>(foregroundDispatcher);

                        var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(foregroundDispatcher, new Lazy<OmniSharp.Extensions.LanguageServer.Protocol.Server.ILanguageServer>(() => server));
                        services.AddSingleton<ProjectSnapshotChangeTrigger>(generatedDocumentPublisher);
                        services.AddSingleton<GeneratedDocumentPublisher>(generatedDocumentPublisher);

                        var documentVersionCache = new DefaultDocumentVersionCache(foregroundDispatcher);
                        services.AddSingleton<DocumentVersionCache>(documentVersionCache);
                        services.AddSingleton<ProjectSnapshotChangeTrigger>(documentVersionCache);
                        var containerStore = new DefaultGeneratedDocumentContainerStore(
                            foregroundDispatcher,
                            documentVersionCache,
                            generatedDocumentPublisher);
                        services.AddSingleton<GeneratedDocumentContainerStore>(containerStore);
                        services.AddSingleton<ProjectSnapshotChangeTrigger>(containerStore);

                        services.AddSingleton<RemoteTextLoaderFactory, DefaultRemoteTextLoaderFactory>();
                        services.AddSingleton<ProjectResolver, DefaultProjectResolver>();
                        services.AddSingleton<DocumentResolver, DefaultDocumentResolver>();
                        services.AddSingleton<RazorProjectService, DefaultRazorProjectService>();
                        services.AddSingleton<ProjectSnapshotChangeTrigger, BackgroundDocumentGenerator>();
                        services.AddSingleton<RazorDocumentMappingService, DefaultRazorDocumentMappingService>();
                        services.AddSingleton<RazorFileChangeDetectorManager>();

                        // Options
                        services.AddSingleton<RazorConfigurationService, DefaultRazorConfigurationService>();
                        services.AddSingleton<RazorLSPOptionsMonitor>();
                        services.AddSingleton<IOptionsMonitor<RazorLSPOptions>, RazorLSPOptionsMonitor>();

                        // File change listeners
                        services.AddSingleton<IProjectConfigurationFileChangeListener, ProjectConfigurationStateSynchronizer>();
                        services.AddSingleton<IProjectFileChangeListener, ProjectFileSynchronizer>();
                        services.AddSingleton<IRazorFileChangeListener, RazorFileSynchronizer>();

                        // File Change detectors
                        services.AddSingleton<IFileChangeDetector, ProjectConfigurationFileChangeDetector>();
                        services.AddSingleton<IFileChangeDetector, ProjectFileChangeDetector>();
                        services.AddSingleton<IFileChangeDetector, RazorFileChangeDetector>();

                        // Document processed listeners
                        services.AddSingleton<DocumentProcessedListener, RazorDiagnosticsPublisher>();
                        services.AddSingleton<DocumentProcessedListener, UnsynchronizableContentDocumentProcessedListener>();

                        services.AddSingleton<HostDocumentFactory, DefaultHostDocumentFactory>();
                        services.AddSingleton<ProjectSnapshotManagerAccessor, DefaultProjectSnapshotManagerAccessor>();
                        services.AddSingleton<TagHelperFactsService, DefaultTagHelperFactsService>();
                        services.AddSingleton<VisualStudio.Editor.Razor.TagHelperCompletionService, VisualStudio.Editor.Razor.DefaultTagHelperCompletionService>();
                        services.AddSingleton<TagHelperDescriptionFactory, DefaultTagHelperDescriptionFactory>();

                        // Completion
                        services.AddSingleton<Completion.TagHelperCompletionService, Completion.DefaultTagHelperCompletionService>();
                        services.AddSingleton<RazorCompletionItemProvider, DirectiveCompletionItemProvider>();
                        services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeCompletionItemProvider>();
                        services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeParameterCompletionItemProvider>();
                        services.AddSingleton<RazorCompletionItemProvider, DirectiveAttributeTransitionCompletionItemProvider>();
                        services.AddSingleton<RazorCompletionItemProvider, MarkupTransitionCompletionItemProvider>();

                        // Auto insert
                        services.AddSingleton<RazorOnAutoInsertProvider, HtmlSmartIndentOnAutoInsertProvider>();
                        services.AddSingleton<RazorOnAutoInsertProvider, CloseRazorCommentOnAutoInsertProvider>();
                        services.AddSingleton<RazorOnAutoInsertProvider, CloseTextTagOnAutoInsertProvider>();
                        services.AddSingleton<RazorOnAutoInsertProvider, AttributeSnippetOnAutoInsertProvider>();

                        // Formatting
                        services.AddSingleton<RazorFormattingService, DefaultRazorFormattingService>();

                        // Formatting Passes
                        services.AddSingleton<IFormattingPass, HtmlFormattingPass>();
                        services.AddSingleton<IFormattingPass, CSharpFormattingPass>();
                        services.AddSingleton<IFormattingPass, CSharpOnTypeFormattingPass>();
                        services.AddSingleton<IFormattingPass, FormattingDiagnosticValidationPass>();
                        services.AddSingleton<IFormattingPass, FormattingContentValidationPass>();

                        // Razor Code actions
                        services.AddSingleton<RazorCodeActionProvider, ExtractToCodeBehindCodeActionProvider>();
                        services.AddSingleton<RazorCodeActionResolver, ExtractToCodeBehindCodeActionResolver>();
                        services.AddSingleton<RazorCodeActionProvider, ComponentAccessibilityCodeActionProvider>();
                        services.AddSingleton<RazorCodeActionResolver, CreateComponentCodeActionResolver>();
                        services.AddSingleton<RazorCodeActionResolver, AddUsingsCodeActionResolver>();

                        // CSharp Code actions
                        services.AddSingleton<CSharpCodeActionProvider, TypeAccessibilityCodeActionProvider>();
                        services.AddSingleton<CSharpCodeActionProvider, ImplementInterfaceAbstractClassCodeActionProvider>();
                        services.AddSingleton<CSharpCodeActionProvider, DefaultCSharpCodeActionProvider>();
                        services.AddSingleton<CSharpCodeActionResolver, DefaultCSharpCodeActionResolver>();

                        // Other
                        services.AddSingleton<RazorCompletionFactsService, DefaultRazorCompletionFactsService>();
                        services.AddSingleton<RazorSemanticTokensInfoService, DefaultRazorSemanticTokensInfoService>();
                        services.AddSingleton<RazorHoverInfoService, DefaultRazorHoverInfoService>();
                        services.AddSingleton<HtmlFactsService, DefaultHtmlFactsService>();
                        services.AddSingleton<WorkspaceDirectoryPathResolver, DefaultWorkspaceDirectoryPathResolver>();
                        services.AddSingleton<RazorComponentSearchEngine, DefaultRazorComponentSearchEngine>();

                        // Defaults: For when the caller hasn't provided them through the `configure` action.
                        services.TryAddSingleton<LanguageServerFeatureOptions, DefaultLanguageServerFeatureOptions>();
                    }));

            try
            {
                var factory = new LoggerFactory();
                var logger = factory.CreateLogger<RazorLanguageServer>();
                var assemblyInformationAttribute = typeof(RazorLanguageServer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                logger.LogInformation("Razor Language Server version " + assemblyInformationAttribute.InformationalVersion);
                factory.Dispose();
            }
            catch
            {
                // Swallow exceptions from determining assembly information.
            }

            var razorLanguageServer = new RazorLanguageServer(server);

            IDisposable exitSubscription = null;
            exitSubscription = server.Exit.Subscribe((_) =>
            {
                exitSubscription.Dispose();
                razorLanguageServer.Dispose();
            });

            return Task.FromResult(razorLanguageServer);
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                {
                    // Already disposed
                    return;
                }

                _disposed = true;

                TempDirectory.Instance.Dispose();
                _innerServer.Dispose();

                // Disposing the server doesn't actually dispose the servers Services for whatever reason. We cast the services collection
                // to IDisposable and try to dispose it ourselves to account for this.
                var disposableServices = _innerServer.Services as IDisposable;
                disposableServices?.Dispose();
            }
        }
    }
}
