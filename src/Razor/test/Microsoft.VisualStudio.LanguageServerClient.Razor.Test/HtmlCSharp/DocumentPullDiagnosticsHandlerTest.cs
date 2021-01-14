// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServerClient.Razor.Feedback;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class DocumentPullDiagnosticsHandlerTest
    {
        private static readonly Diagnostic ValidDiagnostic_UnknownName = new Diagnostic()
        {
            Range = new Range()
            {
                Start = new Position(149, 19),
                End = new Position(149, 23)
            },
            Code = "CS0103",
            Source = "DocumentPullDiagnosticHandler",
            Message = "The name 'saflkjklj' does not exist in the current context"
        };

        private static readonly Range ValidDiagnostic_UnknownName_MappedRange = new Range()
        {
            Start = new Position(49, 19),
            End = new Position(49, 23)
        };

        private static readonly Diagnostic ValidDiagnostic_InvalidExpression = new Diagnostic()
        {
            Range = new Range()
            {
                Start = new Position(150, 19),
                End = new Position(150, 23)
            },
            Code = "CS1525",
            Source = "DocumentPullDiagnosticHandler",
            Message = "Invalid expression term 'bool'"
        };

        private static readonly Range ValidDiagnostic_InvalidExpression_MappedRange = new Range()
        {
            Start = new Position(50, 19),
            End = new Position(50, 23)
        };

        private static readonly Diagnostic UnusedUsingsDiagnostic = new Diagnostic()
        {
            Range = new Range()
            {
                Start = new Position(151, 19),
                End = new Position(151, 23)
            },
            Code = "IDE0005_gen",
            Source = "DocumentPullDiagnosticHandler",
            Message = "Using directive is unnecessary."
        };

        private static readonly Diagnostic RemoveUnnecessaryImportsFixableDiagnostic = new Diagnostic()
        {
            Range = new Range()
            {
                Start = new Position(152, 19),
                End = new Position(152, 23)
            },
            Code = "RemoveUnnecessaryImportsFixable",
            Source = "DocumentPullDiagnosticHandler",
        };

        private static readonly DiagnosticReport[] RoslynDiagnosticResponse = new DiagnosticReport[]
        {
            new DiagnosticReport()
            {
                ResultId = "5",
                Diagnostics = new Diagnostic[]
                {
                    ValidDiagnostic_UnknownName,
                    ValidDiagnostic_InvalidExpression,
                    UnusedUsingsDiagnostic,
                    RemoveUnnecessaryImportsFixableDiagnostic
                }
            }
        };

        public DocumentPullDiagnosticsHandlerTest()
        {
            Uri = new Uri("C:/path/to/file.razor");

            var directoryProvider = new DefaultFeedbackLogDirectoryProvider();
            var loggerFactory = new HTMLCSharpLanguageServerFeedbackFileLoggerProviderFactory(directoryProvider);
            LoggerProvider = new HTMLCSharpLanguageServerFeedbackFileLoggerProvider(loggerFactory);
        }

        private Uri Uri { get; }
        private HTMLCSharpLanguageServerFeedbackFileLoggerProvider LoggerProvider { get; }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var diagnosticsProvider = Mock.Of<LSPDiagnosticsTranslator>();
            var documentSynchronizer = Mock.Of<LSPDocumentSynchronizer>();
            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentSynchronizer, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_RemapsDiagnosticRange()
        {
            // Arrange
            var called = false;
            var documentManager = CreateDocumentManager();

            var requestInvoker = GetRequestInvoker<DocumentDiagnosticsParams, DiagnosticReport[]>(
                RoslynDiagnosticResponse,
                (method, serverContentType, diagnosticParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.DocumentPullDiagnosticName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var diagnosticsProvider = GetDiagnosticsProvider(ValidDiagnostic_UnknownName_MappedRange, ValidDiagnostic_InvalidExpression_MappedRange);
            var documentSynchronizer = CreateDocumentSynchronizer();

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentSynchronizer, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var diagnosticReport = Assert.Single(result);
            Assert.Equal(RoslynDiagnosticResponse.First().ResultId, diagnosticReport.ResultId);
            Assert.Collection(diagnosticReport.Diagnostics,
                d =>
                {
                    Assert.Equal(ValidDiagnostic_UnknownName.Code, d.Code);
                    Assert.Equal(ValidDiagnostic_UnknownName_MappedRange, d.Range);
                },
                d =>
                {
                    Assert.Equal(ValidDiagnostic_InvalidExpression.Code, d.Code);
                    Assert.Equal(ValidDiagnostic_InvalidExpression_MappedRange, d.Range);
                });
        }

        [Fact]
        public async Task HandleRequestAsync_DocumentSynchronizationFails_ReturnsNullDiagnostic()
        {
            // Arrange
            var called = false;
            var documentManager = CreateDocumentManager();

            var requestInvoker = GetRequestInvoker<DocumentDiagnosticsParams, DiagnosticReport[]>(
                RoslynDiagnosticResponse,
                (method, serverContentType, diagnosticParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.DocumentPullDiagnosticName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var diagnosticsProvider = GetDiagnosticsProvider(ValidDiagnostic_UnknownName_MappedRange, ValidDiagnostic_InvalidExpression_MappedRange);

            var documentSynchronizer = new Mock<LSPDocumentSynchronizer>(MockBehavior.Strict);
            documentSynchronizer
                .Setup(d => d.TrySynchronizeVirtualDocumentAsync(It.IsAny<int>(), It.IsAny<CSharpVirtualDocumentSnapshot>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(false));

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentSynchronizer.Object, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.False(called);
            var diagnosticReport = Assert.Single(result);
            Assert.Equal(diagnosticRequest.PreviousResultId, diagnosticReport.ResultId);
            Assert.Null(diagnosticReport.Diagnostics);
        }

        [Fact]
        public async Task HandleRequestAsync_RemapFailsButErrorDiagnosticIsShown()
        {
            // Arrange
            var called = false;
            var documentManager = CreateDocumentManager();

            var unmappableDiagnostic_errorSeverity = new Diagnostic()
            {
                Range = new Range()
                {
                    Start = new Position(149, 19),
                    End = new Position(149, 23)
                },
                Code = "CS0103",
                Severity = DiagnosticSeverity.Error
            };

            var unmappableDiagnostic_warningSeverity = new Diagnostic()
            {
                Range = new Range()
                {
                    Start = new Position(159, 19),
                    End = new Position(159, 23)
                },
                Code = "IDE003",
                Severity = DiagnosticSeverity.Warning
            };

            var diagnosticReport = new DiagnosticReport()
            {
                ResultId = "6",
                Diagnostics = new Diagnostic[]
                {
                    unmappableDiagnostic_errorSeverity,
                    unmappableDiagnostic_warningSeverity
                }
            };

            var requestInvoker = GetRequestInvoker<DocumentDiagnosticsParams, DiagnosticReport[]>(
                new[] { diagnosticReport },
                (method, serverContentType, diagnosticParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.DocumentPullDiagnosticName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var undefinedRange = new Range() { Start = new Position(-1, -1), End = new Position(-1, -1) };
            var diagnosticsProvider = GetDiagnosticsProvider(undefinedRange, undefinedRange);

            var documentSynchronizer = CreateDocumentSynchronizer();

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentSynchronizer, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);

            var diagnosticReportResult = Assert.Single(result);
            Assert.Equal(diagnosticReport.ResultId, diagnosticReportResult.ResultId);

            var returnedDiagnostic = Assert.Single(diagnosticReportResult.Diagnostics);
            Assert.Equal(unmappableDiagnostic_errorSeverity.Code, returnedDiagnostic.Code);
            Assert.True(returnedDiagnostic.Range.IsUndefined());
        }

        [Fact]
        public async Task HandleRequestAsync_NoDiagnosticsAfterFiltering_ReturnsNullDiagnostic()
        {
            // Arrange
            var called = false;
            var documentManager = CreateDocumentManager();

            var filteredDiagnostic = new Diagnostic()
            {
                Range = new Range()
                {
                    Start = new Position(159, 19),
                    End = new Position(159, 23)
                },
                Code = "RemoveUnnecessaryImportsFixable",
                Severity = DiagnosticSeverity.Warning
            };

            var filteredDiagnostic_mappedRange = new Range()
            {
                Start = new Position(49, 19),
                End = new Position(49, 23)
            };

            var diagnosticReport = new DiagnosticReport()
            {
                ResultId = "6",
                Diagnostics = new Diagnostic[]
                {
                    filteredDiagnostic
                }
            };

            var requestInvoker = GetRequestInvoker<DocumentDiagnosticsParams, DiagnosticReport[]>(
                new[] { diagnosticReport },
                (method, serverContentType, diagnosticParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.DocumentPullDiagnosticName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var diagnosticsProvider = GetDiagnosticsProvider(filteredDiagnostic_mappedRange);
            var documentSynchronizer = CreateDocumentSynchronizer();

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentSynchronizer, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var returnedReport = Assert.Single(result);
            Assert.Equal(diagnosticReport.ResultId, returnedReport.ResultId);
            Assert.Empty(returnedReport.Diagnostics);
        }

        [Fact]
        public async Task HandleRequestAsync_VersionMismatch_DiscardsLocation()
        {
            // Arrange
            var called = false;
            var documentManager = CreateDocumentManager(hostDocumentVersion: 1);

            var requestInvoker = GetRequestInvoker<DocumentDiagnosticsParams, DiagnosticReport[]>(
                RoslynDiagnosticResponse,
                (method, serverContentType, diagnosticParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.DocumentPullDiagnosticName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            // Note the HostDocumentVersion provided by the DiagnosticsProvider = 0,
            // which is different from document version (1) from the DocumentManager
            var diagnosticsProvider = GetDiagnosticsProvider(ValidDiagnostic_UnknownName_MappedRange, ValidDiagnostic_InvalidExpression_MappedRange);
            var documentSynchronizer = CreateDocumentSynchronizer();

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentSynchronizer, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var returnedReport = Assert.Single(result);
            Assert.Equal(RoslynDiagnosticResponse.First().ResultId, returnedReport.ResultId);
            Assert.Null(returnedReport.Diagnostics);
        }

        [Fact]
        public async Task HandleRequestAsync_RemapFailure_DiscardsLocation()
        {
            // Arrange
            var called = false;
            var documentManager = CreateDocumentManager();

            var requestInvoker = GetRequestInvoker<DocumentDiagnosticsParams, DiagnosticReport[]>(
                RoslynDiagnosticResponse,
                (method, serverContentType, diagnosticParams, ct) =>
                {
                    Assert.Equal(MSLSPMethods.DocumentPullDiagnosticName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    called = true;
                });

            var diagnosticsProvider = GetDiagnosticsProvider();
            var documentSynchronizer = CreateDocumentSynchronizer();

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentSynchronizer, diagnosticsProvider, LoggerProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            var returnedReport = Assert.Single(result);
            Assert.Equal(RoslynDiagnosticResponse.First().ResultId, returnedReport.ResultId);
            Assert.Null(returnedReport.Diagnostics);
        }

        private LSPRequestInvoker GetRequestInvoker<TParams, TResult>(TResult expectedResponse, Action<string, string, TParams, CancellationToken> callback)
        {
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnMultipleServersAsync<TParams, TResult>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TParams>(), It.IsAny<CancellationToken>()))
                .Callback(callback)
                .Returns(Task.FromResult(new List<TResult>() { expectedResponse } as IEnumerable<TResult>));

            return requestInvoker.Object;
        }

        private TrackingLSPDocumentManager CreateDocumentManager(int hostDocumentVersion = 0)
        {
            var testVirtualDocUri = new Uri("C:/path/to/file.razor.g.cs");
            var testVirtualDocument = new TestVirtualDocumentSnapshot(Uri, hostDocumentVersion);
            var csharpVirtualDocument = new CSharpVirtualDocumentSnapshot(testVirtualDocUri, Mock.Of<ITextSnapshot>(), hostDocumentVersion);
            LSPDocumentSnapshot testDocument = new TestLSPDocumentSnapshot(Uri, hostDocumentVersion, testVirtualDocument, csharpVirtualDocument);
            var documentManager = new Mock<TrackingLSPDocumentManager>(MockBehavior.Strict);
            documentManager.Setup(manager => manager.TryGetDocument(It.IsAny<Uri>(), out testDocument))
                .Returns(true);
            return documentManager.Object;
        }

        private LSPDiagnosticsTranslator GetDiagnosticsProvider(params Range[] expectedRanges)
        {
            var diagnosticsToIgnore = new HashSet<string>()
            {
                "RemoveUnnecessaryImportsFixable",
                "IDE0005_gen", // Using directive is unnecessary
            };

            var diagnosticsProvider = new Mock<LSPDiagnosticsTranslator>(MockBehavior.Strict);
            diagnosticsProvider.Setup(d =>
                d.TranslateAsync(
                    RazorLanguageKind.CSharp,
                    Uri,
                    It.IsAny<Diagnostic[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns((RazorLanguageKind lang, Uri uri, Diagnostic[] diagnostics, CancellationToken ct) =>
                {
                    // Indicates we're mocking mapping failed
                    if (expectedRanges.Length == 0)
                    {
                        return Task.FromResult(new RazorDiagnosticsResponse()
                        {
                            Diagnostics = null,
                            HostDocumentVersion = 0
                        });
                    }

                    var filteredDiagnostics = diagnostics.Where(d => !CanDiagnosticBeFiltered(d));
                    if (!filteredDiagnostics.Any())
                    {
                        return Task.FromResult(new RazorDiagnosticsResponse()
                        {
                            Diagnostics = Array.Empty<Diagnostic>(),
                            HostDocumentVersion = 0
                        });
                    }

                    var mappedDiagnostics = new List<Diagnostic>();

                    for (var i = 0; i < filteredDiagnostics.Count(); i++)
                    {
                        var diagnostic = filteredDiagnostics.ElementAt(i);
                        var range = expectedRanges[i];

                        if (range.IsUndefined())
                        {
                            if (diagnostic.Severity != DiagnosticSeverity.Error)
                            {
                                continue;
                            }
                        }

                        diagnostic.Range = range;
                        mappedDiagnostics.Add(diagnostic);
                    }

                    return Task.FromResult(new RazorDiagnosticsResponse()
                    {
                        Diagnostics = mappedDiagnostics.ToArray(),
                        HostDocumentVersion = 0
                    });

                    bool CanDiagnosticBeFiltered(Diagnostic d) =>
                        (diagnosticsToIgnore.Contains(d.Code) &&
                         d.Severity != DiagnosticSeverity.Error);
                });

            return diagnosticsProvider.Object;
        }

        private static LSPDocumentSynchronizer CreateDocumentSynchronizer()
        {
            var documentSynchronizer = new Mock<LSPDocumentSynchronizer>(MockBehavior.Strict);
            documentSynchronizer
                .Setup(d => d.TrySynchronizeVirtualDocumentAsync(It.IsAny<int>(), It.IsAny<CSharpVirtualDocumentSnapshot>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(true));
            return documentSynchronizer.Object;
        }
    }
}
