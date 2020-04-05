// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.AspNetCore.Razor.LanguageServer.Hover;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.JsonRpc.Serialization.Converters;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Server;
using ILanguageServer = OmniSharp.Extensions.LanguageServer.Server.ILanguageServer;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public sealed class RazorLanguageServer
    {
        private RazorLanguageServer()
        {
        }

        public static Task<ILanguageServer> CreateAsync(Stream input, Stream output, Trace trace)
        {
            Serializer.Instance.JsonSerializer.Converters.RegisterRazorConverters();

            ILanguageServer server = null;
            server = OmniSharp.Extensions.LanguageServer.Server.LanguageServer.PreInit(options =>
                options
                    .WithInput(input)
                    .WithOutput(output)
                    .ConfigureLogging(builder => builder
                        .AddLanguageServer()
                        .SetMinimumLevel(RazorLSPOptions.GetLogLevelForTrace(trace)))
                    .OnInitialized(async (s, request, response) =>
                    {
                        var fileChangeDetectorManager = s.Services.GetRequiredService<RazorFileChangeDetectorManager>();
                        await fileChangeDetectorManager.InitializedAsync(s);

                        // Workaround for https://github.com/OmniSharp/csharp-language-server-protocol/issues/106
                        var languageServer = (OmniSharp.Extensions.LanguageServer.Server.LanguageServer)server;
                        if (request.Capabilities.Workspace.Configuration.IsSupported)
                        {
                            // Initialize our options for the first time.
                            var optionsMonitor = languageServer.Services.GetRequiredService<RazorLSPOptionsMonitor>();
                            _ = Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(async (_) => await optionsMonitor.UpdateAsync());
                        }
                    })
                    .WithHandler<RazorDocumentSynchronizationEndpoint>()
                    .WithHandler<RazorCompletionEndpoint>()
                    .WithHandler<RazorHoverEndpoint>()
                    .WithHandler<RazorLanguageEndpoint>()
                    .WithHandler<RazorConfigurationEndpoint>()
                    .WithHandler<RazorFormattingEndpoint>()
                    .WithHandler<RazorOnTypeFormattingEndpoint>()
                    .WithHandler<RazorSemanticTokenEndpoint>()
                    .WithHandler<RazorSemanticTokenLegendEndpoint>()
                    .WithServices(services =>
                    {
                        services.AddSingleton<FilePathNormalizer>();
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

                        var foregroundDispatcher = new VSCodeForegroundDispatcher();
                        services.AddSingleton<ForegroundDispatcher>(foregroundDispatcher);

                        var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(foregroundDispatcher, new Lazy<OmniSharp.Extensions.LanguageServer.Protocol.Server.ILanguageServer>(() => server));
                        services.AddSingleton<ProjectSnapshotChangeTrigger>(generatedDocumentPublisher);
                        services.AddSingleton<GeneratedDocumentPublisher>(generatedDocumentPublisher);

                        // Formatting
                        services.AddSingleton<RazorFormattingService, DefaultRazorFormattingService>();

                        services.AddSingleton<RazorCompletionFactsService, DefaultRazorCompletionFactsService>();
                        services.AddSingleton<RazorSemanticTokenInfoService, DefaultRazorSemanticTokenInfoService>();
                        services.AddSingleton<RazorHoverInfoService, DefaultRazorHoverInfoService>();
                        services.AddSingleton<HtmlFactsService, DefaultHtmlFactsService>();
                        var documentVersionCache = new DefaultDocumentVersionCache(foregroundDispatcher);
                        services.AddSingleton<DocumentVersionCache>(documentVersionCache);
                        services.AddSingleton<ProjectSnapshotChangeTrigger>(documentVersionCache);
                        var containerStore = new DefaultGeneratedDocumentContainerStore(
                            foregroundDispatcher,
                            documentVersionCache,
                            generatedDocumentPublisher);
                        services.AddSingleton<GeneratedDocumentContainerStore>(containerStore);
                        services.AddSingleton<ProjectSnapshotChangeTrigger>(containerStore);
                    }));

            server.OnShutdown(() =>
            {
                TempDirectory.Instance.Dispose();
                return Unit.Task;
            });

            try
            {
                var factory = new LoggerFactory();
                var logger = factory.CreateLogger<RazorLanguageServer>();
                var assemblyInformationAttribute = typeof(RazorLanguageServer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                logger.LogInformation("Razor Language Server version " + assemblyInformationAttribute.InformationalVersion);
            }
            catch
            {
                // Swallow exceptions from determining assembly information.
            }

            return Task.FromResult(server);
        }
    }
}
