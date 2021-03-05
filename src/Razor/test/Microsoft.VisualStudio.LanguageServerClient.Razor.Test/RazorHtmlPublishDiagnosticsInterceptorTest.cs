// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Logging;
using Microsoft.VisualStudio.Text;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class RazorHtmlPublishDiagnosticsInterceptorTest
    {
        private static readonly Uri RazorUri = new Uri("C:/path/to/file.razor");
        private static readonly Uri CshtmlUri = new Uri("C:/path/to/file.cshtml");
        private static readonly Uri RazorVirtualHtmlUri = new Uri("C:/path/to/file.razor__virtual.html");
        private static readonly Uri RazorVirtualCssUri = new Uri("C:/path/to/file.razor__virtual.css");

        private static readonly Diagnostic ValidDiagnostic_HTML = new Diagnostic()
        {
            Range = new Range()
            {
                Start = new Position(149, 19),
                End = new Position(149, 23)
            },
            Code = null
        };

        private static readonly Diagnostic ValidDiagnostic_CSS = new Diagnostic()
        {
            Range = new Range()
            {
                Start = new Position(150, 19),
                End = new Position(150, 23)
            },
            Code = "expectedSemicolon",
        };

        private static readonly Diagnostic[] Diagnostics = new Diagnostic[]
        {
            ValidDiagnostic_HTML,
            ValidDiagnostic_CSS
        };

        public RazorHtmlPublishDiagnosticsInterceptorTest()
        {
            var logger = new Mock<ILogger>(MockBehavior.Strict).Object;
            Mock.Get(logger).Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception, string>>())).Verifiable();
            LoggerProvider = Mock.Of<HTMLCSharpLanguageServerLogHubLoggerProvider>(l =>
                l.CreateLogger(It.IsAny<string>()) == logger &&
                l.InitializeLoggerAsync(It.IsAny<CancellationToken>()) == Task.CompletedTask,
                MockBehavior.Strict);
        }

        private HTMLCSharpLanguageServerLogHubLoggerProvider LoggerProvider { get; }

        [Fact]
        public async Task ApplyChangesAsync_InvalidParams_ThrowsException()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var diagnosticsProvider = Mock.Of<LSPDiagnosticsTranslator>(MockBehavior.Strict);

            var htmlDiagnosticsInterceptor = new RazorHtmlPublishDiagnosticsInterceptor(documentManager, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new CodeActionParams()
            {
                TextDocument = new TextDocumentIdentifier()
                {
                    Uri = RazorUri
                }
            };

            // Act
            await Assert.ThrowsAnyAsync<ArgumentException>(async () =>
                    await htmlDiagnosticsInterceptor.ApplyChangesAsync(
                        JToken.FromObject(diagnosticRequest),
                        containedLanguageName: string.Empty,
                        cancellationToken: default).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task ApplyChangesAsync_RazorUriNotSupported_ReturnsDefaultResponse()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var diagnosticsProvider = Mock.Of<LSPDiagnosticsTranslator>(MockBehavior.Strict);

            var htmlDiagnosticsInterceptor = new RazorHtmlPublishDiagnosticsInterceptor(documentManager, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new VSPublishDiagnosticParams()
            {
                Diagnostics = Diagnostics,
                Mode = null,
                Uri = RazorUri
            };
            var token = JToken.FromObject(diagnosticRequest);

            // Act
            var result = await htmlDiagnosticsInterceptor.ApplyChangesAsync(token, containedLanguageName: string.Empty, cancellationToken: default).ConfigureAwait(false);

            // Assert
            Assert.Same(token, result.UpdatedToken);
            Assert.False(result.ChangedDocumentUri);
        }

        [Fact]
        public async Task ApplyChangesAsync_CshtmlUriNotSupported_ReturnsDefaultResponse()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var diagnosticsProvider = Mock.Of<LSPDiagnosticsTranslator>(MockBehavior.Strict);

            var htmlDiagnosticsInterceptor = new RazorHtmlPublishDiagnosticsInterceptor(documentManager, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new VSPublishDiagnosticParams()
            {
                Diagnostics = Diagnostics,
                Mode = null,
                Uri = CshtmlUri
            };
            var token = JToken.FromObject(diagnosticRequest);

            // Act
            var result = await htmlDiagnosticsInterceptor.ApplyChangesAsync(token, containedLanguageName: string.Empty, cancellationToken: default).ConfigureAwait(false);

            // Assert
            Assert.Same(token, result.UpdatedToken);
            Assert.False(result.ChangedDocumentUri);
        }

        [Fact]
        public async Task ApplyChangesAsync_CssUriNotSupported_ReturnsDefaultResponse()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var diagnosticsProvider = Mock.Of<LSPDiagnosticsTranslator>(MockBehavior.Strict);

            var htmlDiagnosticsInterceptor = new RazorHtmlPublishDiagnosticsInterceptor(documentManager, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new VSPublishDiagnosticParams()
            {
                Diagnostics = Diagnostics,
                Mode = null,
                Uri = RazorVirtualCssUri
            };
            var token = JToken.FromObject(diagnosticRequest);

            // Act
            var result = await htmlDiagnosticsInterceptor.ApplyChangesAsync(token, containedLanguageName: string.Empty, cancellationToken: default).ConfigureAwait(false);

            // Assert
            Assert.Same(token, result.UpdatedToken);
            Assert.False(result.ChangedDocumentUri);
        }

        [Fact]
        public async Task ApplyChangesAsync_RazorDocumentNotFound_ReturnsEmptyDiagnosticResponse()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var diagnosticsProvider = Mock.Of<LSPDiagnosticsTranslator>(MockBehavior.Strict);

            var htmlDiagnosticsInterceptor = new RazorHtmlPublishDiagnosticsInterceptor(documentManager, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new VSPublishDiagnosticParams()
            {
                Diagnostics = Diagnostics,
                Mode = null,
                Uri = RazorVirtualHtmlUri
            };

            // Act
            var result = await htmlDiagnosticsInterceptor.ApplyChangesAsync(JToken.FromObject(diagnosticRequest), string.Empty, cancellationToken: default).ConfigureAwait(false);

            // Assert
            var updatedParams = result.UpdatedToken.ToObject<VSPublishDiagnosticParams>();
            Assert.Empty(updatedParams.Diagnostics);
            Assert.Equal(RazorUri, updatedParams.Uri);
            Assert.True(result.ChangedDocumentUri);
        }

        [Fact]
        public async Task ApplyChangesAsync_VirtualHtmlDocumentNotFound_ReturnsEmptyDiagnosticResponse()
        {
            // Arrange
            var diagnosticsProvider = Mock.Of<LSPDiagnosticsTranslator>(MockBehavior.Strict);

            var testVirtualDocument = new TestVirtualDocumentSnapshot(RazorUri, hostDocumentVersion: 0);
            LSPDocumentSnapshot testDocument = new TestLSPDocumentSnapshot(RazorUri, version: 0, testVirtualDocument);
            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out testDocument))
                .Returns(true);

            var htmlDiagnosticsInterceptor = new RazorHtmlPublishDiagnosticsInterceptor(documentManager.Object, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new VSPublishDiagnosticParams()
            {
                Diagnostics = Diagnostics,
                Mode = null,
                Uri = RazorVirtualHtmlUri
            };

            // Act
            var result = await htmlDiagnosticsInterceptor.ApplyChangesAsync(JToken.FromObject(diagnosticRequest), string.Empty, cancellationToken: default).ConfigureAwait(false);

            // Assert
            var updatedParams = result.UpdatedToken.ToObject<VSPublishDiagnosticParams>();
            Assert.Empty(updatedParams.Diagnostics);
            Assert.Equal(RazorUri, updatedParams.Uri);
            Assert.True(result.ChangedDocumentUri);
        }

        [Fact]
        public async Task ApplyChangesAsync_EmptyDiagnostics_ReturnsEmptyDiagnosticResponse()
        {
            // Arrange
            var documentManager = CreateDocumentManager();
            var diagnosticsProvider = Mock.Of<LSPDiagnosticsTranslator>(MockBehavior.Strict);

            var htmlDiagnosticsInterceptor = new RazorHtmlPublishDiagnosticsInterceptor(documentManager, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new VSPublishDiagnosticParams()
            {
                Diagnostics = Array.Empty<Diagnostic>(),
                Mode = null,
                Uri = RazorVirtualHtmlUri
            };

            // Act
            var result = await htmlDiagnosticsInterceptor.ApplyChangesAsync(JToken.FromObject(diagnosticRequest), string.Empty, cancellationToken: default).ConfigureAwait(false);

            // Assert
            var updatedParams = result.UpdatedToken.ToObject<VSPublishDiagnosticParams>();
            Assert.Empty(updatedParams.Diagnostics);
            Assert.Equal(RazorUri, updatedParams.Uri);
            Assert.True(result.ChangedDocumentUri);
        }

        [Fact]
        public async Task ApplyChangesAsync_ProcessesDiagnostics_ReturnsDiagnosticResponse()
        {
            // Arrange
            var documentManager = CreateDocumentManager();
            var diagnosticsProvider = GetDiagnosticsProvider();

            var htmlDiagnosticsInterceptor = new RazorHtmlPublishDiagnosticsInterceptor(documentManager, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new VSPublishDiagnosticParams()
            {
                Diagnostics = Diagnostics,
                Mode = null,
                Uri = RazorVirtualHtmlUri
            };

            // Act
            var result = await htmlDiagnosticsInterceptor.ApplyChangesAsync(JToken.FromObject(diagnosticRequest), string.Empty, cancellationToken: default).ConfigureAwait(false);

            // Assert
            var updatedParams = result.UpdatedToken.ToObject<VSPublishDiagnosticParams>();
            Assert.Equal(Diagnostics, updatedParams.Diagnostics);
            Assert.Equal(RazorUri, updatedParams.Uri);
            Assert.True(result.ChangedDocumentUri);
        }

        private static TrackingLSPDocumentManager CreateDocumentManager(int hostDocumentVersion = 0)
        {
            var testVirtualDocUri = RazorVirtualHtmlUri;
            var testVirtualDocument = new TestVirtualDocumentSnapshot(RazorUri, hostDocumentVersion);
            var htmlVirtualDocument = new HtmlVirtualDocumentSnapshot(testVirtualDocUri, Mock.Of<ITextSnapshot>(MockBehavior.Strict), hostDocumentVersion);
            LSPDocumentSnapshot testDocument = new TestLSPDocumentSnapshot(RazorUri, hostDocumentVersion, testVirtualDocument, htmlVirtualDocument);
            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out testDocument))
                .Returns(true);
            return documentManager.Object;
        }

        private LSPDiagnosticsTranslator GetDiagnosticsProvider()
        {
            var diagnosticsToIgnore = new HashSet<string>()
            {
                // N/A For HTML Diagnostics for now
                // https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1257401
            };

            var diagnosticsProvider = new Mock<LSPDiagnosticsTranslator>(MockBehavior.Strict);
            diagnosticsProvider.Setup(d =>
                d.TranslateAsync(
                    RazorLanguageKind.Html,
                    RazorUri,
                    It.IsAny<Diagnostic[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns((RazorLanguageKind lang, Uri uri, Diagnostic[] diagnostics, CancellationToken ct) =>
                {
                    var filteredDiagnostics = diagnostics.Where(d => !CanDiagnosticBeFiltered(d));
                    if (!filteredDiagnostics.Any())
                    {
                        return Task.FromResult(new RazorDiagnosticsResponse()
                        {
                            Diagnostics = Array.Empty<Diagnostic>(),
                            HostDocumentVersion = 0
                        });
                    }

                    return Task.FromResult(new RazorDiagnosticsResponse()
                    {
                        Diagnostics = filteredDiagnostics.ToArray(),
                        HostDocumentVersion = 0
                    });

                    bool CanDiagnosticBeFiltered(Diagnostic d) =>
                        (diagnosticsToIgnore.Contains(d.Code) &&
                         d.Severity != DiagnosticSeverity.Error);
                });

            return diagnosticsProvider.Object;
        }
    }
}
