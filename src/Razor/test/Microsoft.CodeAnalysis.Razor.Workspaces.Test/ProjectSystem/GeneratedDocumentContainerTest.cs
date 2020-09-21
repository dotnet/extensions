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
        public void SetOutput_AcceptsSameVersionedDocuments()
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

            var version = VersionStamp.Create();
            var container = new GeneratedDocumentContainer();
            container.SetOutput(document, csharpDocument, htmlDocument, version, version, version);

            // Act
            container.SetOutput(newDocument, csharpDocument, htmlDocument, version, version, version);

            // Assert
            Assert.Same(newDocument, container.LatestDocument);
        }

        [Fact]
        public void SetOutput_AcceptsInitialOutput()
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

            var version = VersionStamp.Create();
            var container = new GeneratedDocumentContainer();

            // Act
            container.SetOutput(document, csharpDocument, htmlDocument, version, version, version);

            // Assert
            Assert.NotNull(container.LatestDocument);
        }

        [Fact]
        public void SetOutput_InvokesChangedEvent()
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

            var version = VersionStamp.Create();
            var container = new GeneratedDocumentContainer();
            var csharpChanged = false;
            var htmlChanged = false;
            container.GeneratedCSharpChanged += (o, a) => { csharpChanged = true; };
            container.GeneratedHtmlChanged += (o, a) => { htmlChanged = true; };

            // Act
            container.SetOutput(document, csharpDocument, htmlDocument, version, version, version);

            // Assert
            Assert.NotNull(container.LatestDocument);
            Assert.True(csharpChanged);
            Assert.True(htmlChanged);
        }
    }
}
