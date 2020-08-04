// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using static Microsoft.CodeAnalysis.Razor.DocumentExcerptServiceBase;

namespace Microsoft.CodeAnalysis.Razor
{
    public abstract class DocumentExcerptServiceTestBase : WorkspaceTestBase
    {
        private HostProject HostProject { get; }
        private HostDocument HostDocument { get; }

        public DocumentExcerptServiceTestBase()
        {
            HostProject = TestProjectData.SomeProject;
            HostDocument = TestProjectData.SomeProjectFile1;
        }

        public (SourceText sourceText, TextSpan span) CreateText(string text)
        {
            // Since we're using positions, normalize to Windows style
#pragma warning disable CA1307 // Specify StringComparison
            text = text.Replace("\r", "").Replace("\n", "\r\n");

            var start = text.IndexOf('|');
            var length = text.IndexOf('|', start + 1) - start - 1;
            text = text.Replace("|", "");
#pragma warning restore CA1307 // Specify StringComparison

            if (start < 0 || length < 0)
            {
                throw new InvalidOperationException("Could not find delimited text.");
            }

            return (SourceText.From(text), new TextSpan(start, length));
        }

        // Adds the text to a ProjectSnapshot, generates code, and updates the workspace.
        private (DocumentSnapshot primary, Document secondary) InitializeDocument(SourceText sourceText)
        {
            var project = new DefaultProjectSnapshot(
                ProjectState.Create(Workspace.Services, HostProject)
                .WithAddedHostDocument(HostDocument, () =>
                {
                    return Task.FromResult(TextAndVersion.Create(sourceText, VersionStamp.Create()));
                }));

            var primary = project.GetDocument(HostDocument.FilePath);

            var solution = Workspace.CurrentSolution.AddProject(ProjectInfo.Create(
                ProjectId.CreateNewId(Path.GetFileNameWithoutExtension(HostDocument.FilePath)),
                VersionStamp.Create(),
                Path.GetFileNameWithoutExtension(HostDocument.FilePath),
                Path.GetFileNameWithoutExtension(HostDocument.FilePath),
                LanguageNames.CSharp,
                HostDocument.FilePath));

            solution = solution.AddDocument(
                DocumentId.CreateNewId(solution.ProjectIds.Single(), HostDocument.FilePath),
                HostDocument.FilePath,
                new GeneratedDocumentTextLoader(primary, HostDocument.FilePath));

            var secondary = solution.Projects.Single().Documents.Single();
            return (primary, secondary);
        }

        // Maps a span in the primary buffer to the secondary buffer. This is only valid for C# code
        // that appears in the primary buffer.
        private async Task<TextSpan> GetSecondarySpanAsync(DocumentSnapshot primary, TextSpan primarySpan, Document secondary)
        {
            var output = await primary.GetGeneratedOutputAsync();

            var mappings = output.GetCSharpDocument().SourceMappings;
            for (var i = 0; i < mappings.Count; i++)
            {
                var mapping = mappings[i];
                if (mapping.OriginalSpan.AbsoluteIndex <= primarySpan.Start &&
                    (mapping.OriginalSpan.AbsoluteIndex + mapping.OriginalSpan.Length) >= primarySpan.End)
                {
                    var offset = mapping.GeneratedSpan.AbsoluteIndex - mapping.OriginalSpan.AbsoluteIndex;
                    var secondarySpan = new TextSpan(primarySpan.Start + offset, primarySpan.Length);
                    Assert.Equal(
                        (await primary.GetTextAsync()).GetSubText(primarySpan).ToString(),
                        (await secondary.GetTextAsync()).GetSubText(secondarySpan).ToString());
                    return secondarySpan;
                }
            }

            throw new InvalidOperationException("Could not map the primary span to the generated code.");
        }

        public async Task<(Document generatedDocument, SourceText razorSourceText, TextSpan primarySpan, TextSpan generatedSpan)> InitializeAsync(string razorSource)
        {
            var (razorSourceText, primarySpan) = CreateText(razorSource);
            var (primary, generatedDocument) = InitializeDocument(razorSourceText);
            var generatedSpan = await GetSecondarySpanAsync(primary, primarySpan, generatedDocument);
            return (generatedDocument, razorSourceText, primarySpan, generatedSpan);
        }

        internal async Task<(DocumentSnapshot primary, Document generatedDocument, TextSpan generatedSpan)> InitializeWithSnapshotAsync(string razorSource)
        {
            var (razorSourceText, primarySpan) = CreateText(razorSource);
            var (primary, generatedDocument) = InitializeDocument(razorSourceText);
            var generatedSpan = await GetSecondarySpanAsync(primary, primarySpan, generatedDocument);
            return (primary, generatedDocument, generatedSpan);
        }
    }
}
