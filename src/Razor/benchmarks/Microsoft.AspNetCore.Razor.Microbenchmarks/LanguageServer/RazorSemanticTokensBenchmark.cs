// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks.LanguageServer
{
    public class RazorSemanticTokensBenchmark : RazorLanguageServerBenchmarkBase
    {
        private RazorLanguageServer RazorLanguageServer { get; set; }

        private DefaultRazorSemanticTokensInfoService RazorSemanticTokenService { get; set; }

        private DocumentVersionCache VersionCache { get; set; }

        private DocumentUri DocumentUri { get; set; }

        private DocumentSnapshot DocumentSnapshot { get; set; }

        private DocumentSnapshot UpdatedDocumentSnapshot { get; set; }

        private ForegroundDispatcher ForegroundDispatcher { get; set; }

        private string PagesDirectory { get; set; }

        private string ProjectFilePath { get; set; }

        private string TargetPath { get; set; }

        [GlobalSetup(Target = nameof(RazorSemanticTokensEditAsync))]
        public async Task InitializeRazorSemanticAsync()
        {
            await EnsureServicesInitializedAsync();

            var projectRoot = Path.Combine(RepoRoot, "src", "Razor", "test", "testapps", "ComponentApp");
            ProjectFilePath = Path.Combine(projectRoot, "ComponentApp.csproj");
            PagesDirectory = Path.Combine(projectRoot, "Components", "Pages");
            var filePath = Path.Combine(PagesDirectory, $"SemanticTokens.razor");
            TargetPath = "/Components/Pages/SemanticTokens.razor";
            var updatedPath = Path.Combine(PagesDirectory, $"Append.extra");

            DocumentUri = DocumentUri.File(filePath);
            DocumentSnapshot = GetDocumentSnapshot(ProjectFilePath, filePath, TargetPath);
            UpdatedDocumentSnapshot = GetDocumentSnapshot(ProjectFilePath, updatedPath, TargetPath);
        }

        [Benchmark(Description = "Razor Semantic Tokens Formatting")]
        public async Task RazorSemanticTokensEditAsync()
        {
            var textDocumentIdentifier = new TextDocumentIdentifier(DocumentUri);
            var cancellationToken = CancellationToken.None;
            var firstVersion = 1;

            await UpdateDocumentAsync(firstVersion, DocumentSnapshot).ConfigureAwait(false);
            var fullResult = await RazorSemanticTokenService.GetSemanticTokensAsync(textDocumentIdentifier, DocumentSnapshot, firstVersion, range: null, cancellationToken).ConfigureAwait(false);

            var secondVersion = 2;
            await UpdateDocumentAsync(secondVersion, UpdatedDocumentSnapshot).ConfigureAwait(false);
            _ = await RazorSemanticTokenService.GetSemanticTokensEditsAsync(UpdatedDocumentSnapshot, secondVersion, textDocumentIdentifier, fullResult.ResultId, cancellationToken).ConfigureAwait(false);
        }

        private async Task UpdateDocumentAsync(int newVersion, DocumentSnapshot documentSnapshot)
        {
            await Task.Factory.StartNew(() => VersionCache.TrackDocumentVersion(documentSnapshot, newVersion), CancellationToken.None, TaskCreationOptions.None, ForegroundDispatcher.ForegroundScheduler).ConfigureAwait(false);
        }

        [GlobalCleanup]
        public void CleanupServer()
        {
            RazorLanguageServer?.Dispose();
        }

        protected internal override void Builder(RazorLanguageServerBuilder builder)
        {
            builder.Services.AddSingleton<RazorSemanticTokensInfoService, TestRazorSemanticTokensInfoService>();
        }

        private async Task EnsureServicesInitializedAsync()
        {
            if (RazorLanguageServer != null)
            {
                return;
            }

            RazorLanguageServer = await RazorLanguageServerTask.ConfigureAwait(false);
            var languageServer = RazorLanguageServer.GetInnerLanguageServerForTesting();
            RazorSemanticTokenService = languageServer.GetService(typeof(RazorSemanticTokensInfoService)) as TestRazorSemanticTokensInfoService;
            VersionCache = languageServer.GetService(typeof(DocumentVersionCache)) as DocumentVersionCache;
            ForegroundDispatcher = languageServer.GetService(typeof(ForegroundDispatcher)) as ForegroundDispatcher;
        }

        private class TestRazorSemanticTokensInfoService : DefaultRazorSemanticTokensInfoService
        {
            public TestRazorSemanticTokensInfoService(
                ClientNotifierServiceBase languageServer,
                RazorDocumentMappingService documentMappingService,
                ForegroundDispatcher foregroundDispatcher,
                DocumentResolver documentResolver,
                DocumentVersionCache documentVersionCache,
                LoggerFactory loggerFactory) :
                base(languageServer, documentMappingService, foregroundDispatcher, documentResolver, documentVersionCache, loggerFactory)
            {
            }

            // We can't get C# responses without significant amounts of extra work, so let's just shim it for now, any non-Null result is fine.
            internal override Task<IReadOnlyList<SemanticRange>> GetCSharpSemanticRangesAsync(RazorCodeDocument codeDocument, TextDocumentIdentifier textDocumentIdentifier, Range range, long? documentVersion, CancellationToken cancellationToken)
            {
                return Task.FromResult((IReadOnlyList<SemanticRange>)new List<SemanticRange>());
            }
        }
    }
}
