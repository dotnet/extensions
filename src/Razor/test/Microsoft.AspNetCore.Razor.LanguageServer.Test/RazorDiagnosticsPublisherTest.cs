// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;
using Xunit.Sdk;
using RazorDiagnosticFactory = Microsoft.AspNetCore.Razor.Language.RazorDiagnosticFactory;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorDiagnosticsPublisherTest : LanguageServerTestBase
    {
        public RazorDiagnosticsPublisherTest()
        {
            var testProjectManager = TestProjectSnapshotManager.Create(Dispatcher);
            var hostProject = new HostProject("/C:/project/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
            testProjectManager.ProjectAdded(hostProject);
            var sourceText = SourceText.From(string.Empty);
            var textAndVersion = TextAndVersion.Create(sourceText, VersionStamp.Default);
            var openedHostDocument = new HostDocument("/C:/project/open_document.cshtml", "/C:/project/open_document.cshtml");
            testProjectManager.DocumentAdded(hostProject, openedHostDocument, TextLoader.From(textAndVersion));
            testProjectManager.DocumentOpened(hostProject.FilePath, openedHostDocument.FilePath, sourceText);
            var closedHostDocument = new HostDocument("/C:/project/closed_document.cshtml", "/C:/project/closed_document.cshtml");
            testProjectManager.DocumentAdded(hostProject, closedHostDocument, TextLoader.From(textAndVersion));

            OpenedDocument = testProjectManager.Projects[0].GetDocument(openedHostDocument.FilePath);
            ClosedDocument = testProjectManager.Projects[0].GetDocument(closedHostDocument.FilePath);
            ProjectManager = testProjectManager;
        }

        private ProjectSnapshotManager ProjectManager { get; }

        private DocumentSnapshot ClosedDocument { get; }

        private DocumentSnapshot OpenedDocument { get; }

        private RazorDiagnostic[] EmptyDiagnostics => Array.Empty<RazorDiagnostic>();

        private RazorDiagnostic[] SingleDiagnosticCollection => new RazorDiagnostic[]
        {
            RazorDiagnosticFactory.CreateDirective_BlockDirectiveCannotBeImported("test")
        };

        [Fact]
        public async Task PublishDiagnosticsAsync_NewDocumentDiagnosticsGetPublished()
        {
            // Arrange
            var processedOpenDocument = TestDocumentSnapshot.Create(OpenedDocument.FilePath);
            var codeDocument = CreateCodeDocument(SingleDiagnosticCollection);
            processedOpenDocument.With(codeDocument);
            var languageServerDocument = new Mock<ILanguageServerDocument>();
            languageServerDocument.Setup(lsd => lsd.SendNotification(It.IsAny<string>(), It.IsAny<PublishDiagnosticsParams>()))
                .Callback<string, PublishDiagnosticsParams>((method, diagnosticParams) =>
                {
                    Assert.Equal(processedOpenDocument.FilePath.TrimStart('/'), diagnosticParams.Uri.AbsolutePath);
                    var diagnostic = Assert.Single(diagnosticParams.Diagnostics);
                    var razorDiagnostic = SingleDiagnosticCollection[0];
                    processedOpenDocument.TryGetText(out var sourceText);
                    var expectedDiagnostic = RazorDiagnosticConverter.Convert(razorDiagnostic, sourceText);
                    Assert.Equal(expectedDiagnostic.Message, diagnostic.Message);
                    Assert.Equal(expectedDiagnostic.Severity, diagnostic.Severity);
                    Assert.Equal(expectedDiagnostic.Range, diagnostic.Range);
                }).Verifiable();
            var languageServer = new Mock<ILanguageServer>();
            languageServer.Setup(server => server.Document).Returns(languageServerDocument.Object);
            using (var publisher = new TestRazorDiagnosticsPublisher(Dispatcher, languageServer.Object, LoggerFactory))
            {
                publisher.Initialize(ProjectManager);

                // Act
                await publisher.PublishDiagnosticsAsync(processedOpenDocument);

                // Assert
                languageServerDocument.VerifyAll();
            }
        }

        [Fact]
        public async Task PublishDiagnosticsAsync_NewDiagnosticsGetPublished()
        {
            // Arrange
            var processedOpenDocument = TestDocumentSnapshot.Create(OpenedDocument.FilePath);
            var codeDocument = CreateCodeDocument(SingleDiagnosticCollection);
            processedOpenDocument.With(codeDocument);
            var languageServerDocument = new Mock<ILanguageServerDocument>();
            languageServerDocument.Setup(lsd => lsd.SendNotification(It.IsAny<string>(), It.IsAny<PublishDiagnosticsParams>()))
                .Callback<string, PublishDiagnosticsParams>((method, diagnosticParams) =>
                {
                    Assert.Equal(processedOpenDocument.FilePath.TrimStart('/'), diagnosticParams.Uri.AbsolutePath);
                    var diagnostic = Assert.Single(diagnosticParams.Diagnostics);
                    var razorDiagnostic = SingleDiagnosticCollection[0];
                    processedOpenDocument.TryGetText(out var sourceText);
                    var expectedDiagnostic = RazorDiagnosticConverter.Convert(razorDiagnostic, sourceText);
                    Assert.Equal(expectedDiagnostic.Message, diagnostic.Message);
                    Assert.Equal(expectedDiagnostic.Severity, diagnostic.Severity);
                    Assert.Equal(expectedDiagnostic.Range, diagnostic.Range);
                }).Verifiable();
            var languageServer = new Mock<ILanguageServer>();
            languageServer.Setup(server => server.Document).Returns(languageServerDocument.Object);
            using (var publisher = new TestRazorDiagnosticsPublisher(Dispatcher, languageServer.Object, LoggerFactory))
            {
                publisher._publishedDiagnostics[processedOpenDocument.FilePath] = EmptyDiagnostics;
                publisher.Initialize(ProjectManager);

                // Act
                await publisher.PublishDiagnosticsAsync(processedOpenDocument);

                // Assert
                languageServerDocument.VerifyAll();
            }
        }

        [Fact]
        public async Task PublishDiagnosticsAsync_NoopsIfDiagnosticsAreSameAsPreviousPublish()
        {
            // Arrange
            var languageServer = new Mock<ILanguageServer>();
            languageServer.Setup(server => server.Document).Throws<XunitException>();
            var processedOpenDocument = TestDocumentSnapshot.Create(OpenedDocument.FilePath);
            var codeDocument = CreateCodeDocument(SingleDiagnosticCollection);
            processedOpenDocument.With(codeDocument);
            using (var publisher = new TestRazorDiagnosticsPublisher(Dispatcher, languageServer.Object, LoggerFactory))
            {
                publisher._publishedDiagnostics[processedOpenDocument.FilePath] = SingleDiagnosticCollection;
                publisher.Initialize(ProjectManager);

                // Act & Assert
                await publisher.PublishDiagnosticsAsync(processedOpenDocument);
            }
        }

        [Fact]
        public void ClearClosedDocuments_ClearsDiagnosticsForClosedDocument()
        {
            // Arrange
            var languageServerDocument = new Mock<ILanguageServerDocument>();
            languageServerDocument.Setup(lsd => lsd.SendNotification(It.IsAny<string>(), It.IsAny<PublishDiagnosticsParams>()))
                .Callback<string, PublishDiagnosticsParams>((method, diagnosticParams) =>
                {
                    Assert.Equal(ClosedDocument.FilePath.TrimStart('/'), diagnosticParams.Uri.AbsolutePath);
                    Assert.Empty(diagnosticParams.Diagnostics);
                }).Verifiable();
            var languageServer = new Mock<ILanguageServer>();
            languageServer.Setup(server => server.Document).Returns(languageServerDocument.Object);
            using (var publisher = new TestRazorDiagnosticsPublisher(Dispatcher, languageServer.Object, LoggerFactory))
            {
                publisher._publishedDiagnostics[ClosedDocument.FilePath] = SingleDiagnosticCollection;
                publisher.Initialize(ProjectManager);

                // Act
                publisher.ClearClosedDocuments();

                // Assert
                languageServerDocument.VerifyAll();
            }
        }

        [Fact]
        public void ClearClosedDocuments_NoopsIfDocumentIsStillOpen()
        {
            // Arrange
            var languageServer = new Mock<ILanguageServer>();
            languageServer.Setup(server => server.Document).Throws<XunitException>();
            using (var publisher = new TestRazorDiagnosticsPublisher(Dispatcher, languageServer.Object, LoggerFactory))
            {
                publisher._publishedDiagnostics[OpenedDocument.FilePath] = SingleDiagnosticCollection;
                publisher.Initialize(ProjectManager);

                // Act & Assert
                publisher.ClearClosedDocuments();
            }
        }

        [Fact]
        public void ClearClosedDocuments_NoopsIfDocumentIsClosedButNoDiagnostics()
        {
            // Arrange
            var languageServer = new Mock<ILanguageServer>();
            languageServer.Setup(server => server.Document).Throws<XunitException>();
            using (var publisher = new TestRazorDiagnosticsPublisher(Dispatcher, languageServer.Object, LoggerFactory))
            {
                publisher._publishedDiagnostics[ClosedDocument.FilePath] = EmptyDiagnostics;
                publisher.Initialize(ProjectManager);

                // Act & Assert
                publisher.ClearClosedDocuments();
            }
        }

        [Fact]
        public void ClearClosedDocuments_RestartsTimerIfDocumentsStillOpen()
        {
            // Arrange
            var languageServer = new Mock<ILanguageServer>();
            languageServer.Setup(server => server.Document).Throws<XunitException>();
            using (var publisher = new TestRazorDiagnosticsPublisher(Dispatcher, languageServer.Object, LoggerFactory))
            {
                publisher._publishedDiagnostics[ClosedDocument.FilePath] = EmptyDiagnostics;
                publisher._publishedDiagnostics[OpenedDocument.FilePath] = EmptyDiagnostics;
                publisher.Initialize(ProjectManager);

                // Act
                publisher.ClearClosedDocuments();

                // Assert
                Assert.NotNull(publisher._documentClosedTimer);
            }
        }

        private static RazorCodeDocument CreateCodeDocument(params RazorDiagnostic[] diagnostics)
        {
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var razorCSharpDocument = RazorCSharpDocument.Create(string.Empty, RazorCodeGenerationOptions.CreateDefault(), diagnostics);
            codeDocument.SetCSharpDocument(razorCSharpDocument);

            return codeDocument;
        }

        private class TestRazorDiagnosticsPublisher : RazorDiagnosticsPublisher, IDisposable
        {
            public TestRazorDiagnosticsPublisher(
                ForegroundDispatcher foregroundDispatcher,
                ILanguageServer languageServer,
                ILoggerFactory loggerFactory) : base(foregroundDispatcher, languageServer, loggerFactory)
            {
            }

            public void Dispose()
            {
                _workTimer?.Dispose();
                _documentClosedTimer?.Dispose();
            }
        }
    }
}
