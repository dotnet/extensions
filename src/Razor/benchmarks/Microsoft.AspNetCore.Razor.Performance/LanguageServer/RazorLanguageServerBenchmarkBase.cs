// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Microsoft.AspNetCore.Razor.Performance.LanguageServer
{
    public class RazorLanguageServerBenchmarkBase : ProjectSnapshotManagerBenchmarkBase
    {
        public RazorLanguageServerBenchmarkBase()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && !File.Exists(Path.Combine(current.FullName, "src", "Razor", "Razor.sln")))
            {
                current = current.Parent;
            }
            RepoRoot = current.FullName;

            using var memoryStream = new MemoryStream();
            RazorLanguageServerTask = RazorLanguageServer.CreateAsync(memoryStream, memoryStream, Trace.Off, builder =>
            {
                builder.Services.AddSingleton<ClientNotifierServiceBase, NoopClientNotifierService>();
            });
        }

        protected string RepoRoot { get; }

        protected Task<RazorLanguageServer> RazorLanguageServerTask { get; }

        internal DocumentSnapshot GetDocumentSnapshot(string projectFilePath, string filePath, string targetPath)
        {
            var hostProject = new HostProject(projectFilePath, RazorConfiguration.Default, rootNamespace: null);
            using var fileStream = new FileStream(filePath, FileMode.Open);
            var text = SourceText.From(fileStream);
            var textLoader = TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create()));
            var hostDocument = new HostDocument(filePath, targetPath, FileKinds.Component);

            var projectSnapshotManager = CreateProjectSnapshotManager();
            projectSnapshotManager.ProjectAdded(hostProject);
            projectSnapshotManager.DocumentAdded(hostProject, hostDocument, textLoader);
            var projectSnapshot = projectSnapshotManager.GetOrCreateProject(projectFilePath);

            var documentSnapshot = projectSnapshot.GetDocument(filePath);
            return documentSnapshot;
        }

        private class NoopClientNotifierService : ClientNotifierServiceBase
        {
            public override InitializeParams ClientSettings => new InitializeParams();

            public override Task OnStarted(ILanguageServer server, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public override Task<IResponseRouterReturns> SendRequestAsync(string method)
            {
                return Task.FromResult<IResponseRouterReturns>(new NoopResponse());
            }

            public override Task<IResponseRouterReturns> SendRequestAsync<T>(string method, T @params)
            {
                return Task.FromResult<IResponseRouterReturns>(new NoopResponse());
            }

            class NoopResponse : IResponseRouterReturns
            {
                public Task<TResponse> Returning<TResponse>(CancellationToken cancellationToken)
                {
                    return Task.FromResult(Activator.CreateInstance<TResponse>());
                }

                public Task ReturningVoid(CancellationToken cancellationToken)
                {
                    return Task.CompletedTask;
                }
            }
        }
    }
}
