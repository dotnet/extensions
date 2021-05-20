// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class GeneratedDocumentContainerTest : RazorProjectEngineTestBase
    {
        protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

        [Fact]
        public void TrySetOutput_AcceptsSameVersionedDocuments()
        {
            // Arrange
            using var workspace = TestWorkspace.Create();

            var services = workspace.Services;
            var hostProject = new HostProject("C:/project.csproj", RazorConfiguration.Default, "project");
            var projectState = ProjectState.Create(services, hostProject);
            var project = new DefaultProjectSnapshot(projectState);

            var text = SourceText.From("...");
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Default);
            var hostDocument = new HostDocument("C:/file.cshtml", "C:/file.cshtml");
            var documentState = new DocumentState(services, hostDocument, text, VersionStamp.Default, () => Task.FromResult(textAndVersion));
            var document = new DefaultDocumentSnapshot(project, documentState);
            var newDocument = new DefaultDocumentSnapshot(project, documentState);

            var csharpDocument = RazorCSharpDocument.Create("...", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());
            var htmlDocument = RazorHtmlDocument.Create("...", RazorCodeGenerationOptions.CreateDefault());
            var codeDocument = CreateCodeDocument(csharpDocument, htmlDocument);

            var version = VersionStamp.Create();
            var container = new GeneratedDocumentContainer();
            var initialResult = container.TrySetOutput(document, codeDocument, version, version, version);

            // Act
            var result = container.TrySetOutput(newDocument, codeDocument, version, version, version);

            // Assert
            Assert.Same(newDocument, container.LatestDocument);
            Assert.True(initialResult);
            Assert.True(result);
        }

        [Fact]
        public void TrySetOutput_AcceptsInitialOutput()
        {
            // Arrange
            using var workspace = TestWorkspace.Create();

            var services = workspace.Services;
            var hostProject = new HostProject("C:/project.csproj", RazorConfiguration.Default, "project");
            var projectState = ProjectState.Create(services, hostProject);
            var project = new DefaultProjectSnapshot(projectState);

            var text = SourceText.From("...");
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Default);
            var hostDocument = new HostDocument("C:/file.cshtml", "C:/file.cshtml");
            var documentState = new DocumentState(services, hostDocument, text, VersionStamp.Default, () => Task.FromResult(textAndVersion));
            var document = new DefaultDocumentSnapshot(project, documentState);
            var csharpDocument = RazorCSharpDocument.Create("...", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());
            var htmlDocument = RazorHtmlDocument.Create("...", RazorCodeGenerationOptions.CreateDefault());
            var codeDocument = CreateCodeDocument(csharpDocument, htmlDocument);

            var version = VersionStamp.Create();
            var container = new GeneratedDocumentContainer();

            // Act
            var result = container.TrySetOutput(document, codeDocument, version, version, version);

            // Assert
            Assert.NotNull(container.LatestDocument);
            Assert.True(result);
        }

        [Fact]
        public void TrySetOutput_InvokesChangedEvent()
        {
            // Arrange
            using var workspace = TestWorkspace.Create();

            var services = workspace.Services;
            var hostProject = new HostProject("C:/project.csproj", RazorConfiguration.Default, "project");
            var projectState = ProjectState.Create(services, hostProject);
            var project = new DefaultProjectSnapshot(projectState);

            var text = SourceText.From("...");
            var textAndVersion = TextAndVersion.Create(text, VersionStamp.Default);
            var hostDocument = new HostDocument("C:/file.cshtml", "C:/file.cshtml");
            var documentState = new DocumentState(services, hostDocument, text, VersionStamp.Default, () => Task.FromResult(textAndVersion));
            var document = new DefaultDocumentSnapshot(project, documentState);
            var csharpDocument = RazorCSharpDocument.Create("...", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());
            var htmlDocument = RazorHtmlDocument.Create("...", RazorCodeGenerationOptions.CreateDefault());
            var codeDocument = CreateCodeDocument(csharpDocument, htmlDocument);

            var version = VersionStamp.Create();
            var container = new GeneratedDocumentContainer();
            var csharpChanged = false;
            var htmlChanged = false;
            container.GeneratedCSharpChanged += (o, a) => csharpChanged = true;
            container.GeneratedHtmlChanged += (o, a) => htmlChanged = true;

            // Act
            var result = container.TrySetOutput(document, codeDocument, version, version, version);

            // Assert
            Assert.NotNull(container.LatestDocument);
            Assert.True(csharpChanged);
            Assert.True(htmlChanged);
            Assert.True(result);
        }

        private static RazorCodeDocument CreateCodeDocument(RazorCSharpDocument csharpDocument, RazorHtmlDocument htmlDocument)
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            codeDocument.SetCSharpDocument(csharpDocument);
            codeDocument.Items[typeof(RazorHtmlDocument)] = htmlDocument;
            return codeDocument;
        }
    }
}
