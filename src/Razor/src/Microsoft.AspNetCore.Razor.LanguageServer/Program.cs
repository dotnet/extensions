// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Common.Serialization;
using Microsoft.AspNetCore.Razor.LanguageServer.Completion;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Editor.Razor;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).Wait();
        }

        public static async Task MainAsync(string[] args)
        {
            var logLevel = LogLevel.Information;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i].IndexOf("debug", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    while (!Debugger.IsAttached)
                    {
                        Thread.Sleep(1000);
                    }

                    Debugger.Break();
                    continue;
                }

                if (args[i] == "--logLevel" && i + 1 < args.Length)
                {
                    var logLevelString = args[++i];
                    if (!Enum.TryParse(logLevelString, out logLevel))
                    {
                        logLevel = LogLevel.Information;
                        Console.WriteLine($"Invalid log level '{logLevelString}'. Defaulting to {logLevel.ToString()}.");
                    }
                }
            }

            Serializer.Instance.JsonSerializer.Converters.RegisterRazorConverters();

            var factory = new LoggerFactory();
            ILanguageServer server = null;
            server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options =>
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .WithLoggerFactory(factory)
                    .AddDefaultLoggingProvider()
                    .WithMinimumLogLevel(logLevel)
                    .WithHandler<RazorDocumentSynchronizationEndpoint>()
                    .WithHandler<RazorCompletionEndpoint>()
                    .WithHandler<RazorLanguageEndpoint>()
                    .WithHandler<RazorProjectEndpoint>()
                    .WithServices(services =>
                    {
                        services.AddSingleton<RemoteTextLoaderFactory, DefaultRemoteTextLoaderFactory>();
                        services.AddSingleton<ProjectResolver, DefaultProjectResolver>();
                        services.AddSingleton<DocumentResolver, DefaultDocumentResolver>();
                        services.AddSingleton<FilePathNormalizer>();
                        services.AddSingleton<RazorProjectService, DefaultRazorProjectService>();
                        services.AddSingleton<ProjectSnapshotChangeTrigger, BackgroundDocumentGenerator>();

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

                        var foregroundDispatcher = new VSCodeForegroundDispatcher();
                        services.AddSingleton<ForegroundDispatcher>(foregroundDispatcher);
                        services.AddSingleton<RazorCompletionFactsService, DefaultRazorCompletionFactsService>();
                        var documentVersionCache = new DefaultDocumentVersionCache(foregroundDispatcher);
                        services.AddSingleton<DocumentVersionCache>(documentVersionCache);
                        services.AddSingleton<ProjectSnapshotChangeTrigger>(documentVersionCache);
                        var containerStore = new DefaultGeneratedCodeContainerStore(
                            foregroundDispatcher,
                            documentVersionCache,
                            new Lazy<ILanguageServer>(() => server));
                        services.AddSingleton<GeneratedCodeContainerStore>(containerStore);
                        services.AddSingleton<ProjectSnapshotChangeTrigger>(containerStore);
                    }));

            // Workaround for https://github.com/OmniSharp/csharp-language-server-protocol/issues/106
            var languageServer = (OmniSharp.Extensions.LanguageServer.Server.LanguageServer)server;
            languageServer.MinimumLogLevel = logLevel;

            try
            {
                var logger = factory.CreateLogger<Program>();
                var assemblyInformationAttribute = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                logger.LogInformation("Razor Language Server version " + assemblyInformationAttribute.InformationalVersion);
            }
            catch
            {
                // Swallow exceptions from determining assembly information.
            }

            await server.WaitForExit;

            TempDirectory.Instance.Dispose();
        }
    }
}
