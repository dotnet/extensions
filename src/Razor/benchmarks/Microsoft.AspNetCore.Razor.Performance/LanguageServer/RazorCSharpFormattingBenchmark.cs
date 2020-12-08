// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.LanguageServer;
using Microsoft.AspNetCore.Razor.LanguageServer.Formatting;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Extensions.LanguageServer.Protocol;
using FormattingOptions = OmniSharp.Extensions.LanguageServer.Protocol.Models.FormattingOptions;

namespace Microsoft.AspNetCore.Razor.Performance.LanguageServer
{
    public class RazorCSharpFormattingBenchmark : RazorLanguageServerBenchmarkBase
    {
        private RazorLanguageServer RazorLanguageServer { get; set; }

        private RazorFormattingService RazorFormattingService { get; set; }

        private DocumentUri DocumentUri { get; set; }

        private DocumentSnapshot DocumentSnapshot { get; set; }

        private SourceText DocumentText { get; set; }

        [GlobalSetup(Target = nameof(RazorCSharpFormattingAsync))]
        public async Task InitializeRazorCSharpFormattingAsync()
        {
            await EnsureServicesInitializedAsync();

            var projectRoot = Path.Combine(RepoRoot, "src", "Razor", "test", "testapps", "ComponentApp");
            var projectFilePath = Path.Combine(projectRoot, "ComponentApp.csproj");
            var filePath = Path.Combine(projectRoot, "Components", "Pages", $"FormattingTest.razor");
            var targetPath = "/Components/Pages/FormattingTest.razor";

            DocumentUri = DocumentUri.File(filePath);
            DocumentSnapshot = GetDocumentSnapshot(projectFilePath, filePath, targetPath);
            DocumentText = await DocumentSnapshot.GetTextAsync();
        }

        [Benchmark(Description = "Razor CSharp Formatting")]
        public async Task RazorCSharpFormattingAsync()
        {
            var options = new FormattingOptions()
            {
                TabSize = 4,
                InsertSpaces = true
            };

            var range = TextSpan.FromBounds(0, DocumentText.Length).AsRange(DocumentText);
            var edits = await RazorFormattingService.FormatAsync(DocumentUri, DocumentSnapshot, range, options, CancellationToken.None);

#if DEBUG
            // For debugging purposes only.
            var changedText = DocumentText.WithChanges(edits.Select(e => e.AsTextChange(DocumentText)));
            _ = changedText.ToString();
#endif
        }

        [GlobalCleanup]
        public void CleanupServer()
        {
            RazorLanguageServer?.Dispose();
        }

        private async Task EnsureServicesInitializedAsync()
        {
            if (RazorLanguageServer != null)
            {
                return;
            }

            RazorLanguageServer = await RazorLanguageServerTask;
            var languageServer = RazorLanguageServer.GetInnerLanguageServerForTesting();
            RazorFormattingService = languageServer.GetService(typeof(RazorFormattingService)) as RazorFormattingService;
        }
    }
}
