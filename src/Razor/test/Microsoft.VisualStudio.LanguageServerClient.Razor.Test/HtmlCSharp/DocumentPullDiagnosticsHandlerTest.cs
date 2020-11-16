// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
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
        }

        private Uri Uri { get; }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>();
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();
            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentMappingProvider);
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

            var documentMappingProvider = GetDocumentMappingProvider(ValidDiagnostic_UnknownName_MappedRange, ValidDiagnostic_InvalidExpression_MappedRange);

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentMappingProvider);
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

            // Note the HostDocumentVersion provided by the DocumentMappingProvider = 0,
            // which is different from document version (1) from the DocumentManager
            var documentMappingProvider = GetDocumentMappingProvider(ValidDiagnostic_UnknownName_MappedRange, ValidDiagnostic_InvalidExpression_MappedRange);

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentMappingProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Empty(result);
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

            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>();

            var documentDiagnosticsHandler = new DocumentPullDiagnosticsHandler(requestInvoker, documentManager, documentMappingProvider);
            var diagnosticRequest = new DocumentDiagnosticsParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                PreviousResultId = "4"
            };

            // Act
            var result = await documentDiagnosticsHandler.HandleRequestAsync(diagnosticRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(called);
            Assert.Empty(result);
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

        private LSPDocumentMappingProvider GetDocumentMappingProvider(params Range[] expectedRanges)
        {
            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = expectedRanges,
                HostDocumentVersion = 0
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, Uri, It.IsAny<Range[]>(), LanguageServerMappingBehavior.Inclusive, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            return documentMappingProvider.Object;
        }
    }
}
