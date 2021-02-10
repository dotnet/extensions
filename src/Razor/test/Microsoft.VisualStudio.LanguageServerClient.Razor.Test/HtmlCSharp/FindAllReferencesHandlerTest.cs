// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Threading;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class FindAllReferencesHandlerTest
    {
        public FindAllReferencesHandlerTest()
        {
            Uri = new Uri("C:/path/to/file.razor");

            // Long timeout after last notification to avoid triggering even in slow CI environments
            TestWaitForProgressNotificationTimeout = TimeSpan.FromSeconds(30);
        }

        private Uri Uri { get; }
        private TimeSpan TestWaitForProgressNotificationTimeout { get; }

        [Fact]
        public async Task HandleRequestAsync_DocumentNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            var requestInvoker = Mock.Of<LSPRequestInvoker>(MockBehavior.Strict);
            var projectionProvider = Mock.Of<LSPProjectionProvider>(MockBehavior.Strict);
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>(MockBehavior.Strict);
            var progressListener = Mock.Of<LSPProgressListener>(MockBehavior.Strict);
            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_ProjectionNotFound_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));
            var requestInvoker = Mock.Of<LSPRequestInvoker>(MockBehavior.Strict);
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict).Object;
            Mock.Get(projectionProvider).Setup(projectionProvider => projectionProvider.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), CancellationToken.None))
                .Returns(Task.FromResult<ProjectionResult>(null));
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>(MockBehavior.Strict);
            var progressListener = Mock.Of<LSPProgressListener>(MockBehavior.Strict);
            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider, documentMappingProvider, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_HtmlProjection_InvokesHtmlLanguageServer()
        {
            // Arrange
            var lspFarEndpointCalled = false;
            var progressReported = false;
            var expectedUri1 = new Uri("C:/path/to/file1.razor");
            var expectedUri2 = new Uri("C:/path/to/file2.razor");
            var expectedLocation1 = GetReferenceItem(5, expectedUri1);
            var expectedLocation2 = GetReferenceItem(10, expectedUri2);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));

            var virtualHtmlUri1 = new Uri("C:/path/to/file1.razor__virtual.html");
            var virtualHtmlUri2 = new Uri("C:/path/to/file2.razor__virtual.html");
            var htmlLocation1 = GetReferenceItem(100, virtualHtmlUri1);
            var htmlLocation2 = GetReferenceItem(200, virtualHtmlUri2);

            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);
            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            var token = Guid.NewGuid().ToString();
            var parameterToken = new JObject
            {
                { "token", token },
                { "value", JArray.FromObject(new[] { htmlLocation1, htmlLocation2 }) }
            };

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, VSReferenceItem[]>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, TextDocumentPositionParams, CancellationToken>((method, serverContentType, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentReferencesName, method);
                    Assert.Equal(RazorLSPConstants.HtmlLSPContentTypeName, serverContentType);
                    lspFarEndpointCalled = true;

                    _ = lspProgressListener.ProcessProgressNotificationAsync(Methods.ProgressNotificationName, parameterToken);
                })
                .Returns(Task.FromResult(Array.Empty<VSReferenceItem>()));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.Html,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult1 = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation1.Location.Range }
            };
            var remappingResult2 = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation2.Location.Range }
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider
                .Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.Html, It.IsAny<Uri>(), It.IsAny<Range[]>(), It.IsAny<CancellationToken>()))
                .Returns<RazorLanguageKind, Uri, Range[], CancellationToken>((languageKind, uri, ranges, ct) => Task.FromResult(uri.LocalPath.Contains("file1") ? remappingResult1 : remappingResult2));

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object, lspProgressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                Assert.Collection(results,
                    a => AssertVSReferenceItem(expectedLocation1, a),
                    b => AssertVSReferenceItem(expectedLocation2, b));
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(lspFarEndpointCalled);
            Assert.True(progressReported);
        }

        [Fact]
        public async Task HandleRequestAsync_ProgressListener_ReturnsNull()
        {
            // Arrange
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));
            var requestInvoker = Mock.Of<LSPRequestInvoker>(MockBehavior.Strict);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            Task onCompleted = null;
            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>(MockBehavior.Strict);
            var progressListener = Mock.Of<LSPProgressListener>(l =>
                l.TryListenForProgress(
                    It.IsAny<string>(),
                    It.IsAny<Func<JToken, CancellationToken, Task>>(),
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>(),
                    out onCompleted) == false, MockBehavior.Strict);

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(0, 1)
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_InvokesCSharpLanguageServer()
        {
            // Arrange
            var lspFarEndpointCalled = false;
            var progressReported = false;
            var expectedUri1 = new Uri("C:/path/to/file1.razor");
            var expectedUri2 = new Uri("C:/path/to/file2.razor");
            var expectedLocation1 = GetReferenceItem(5, expectedUri1);
            var expectedLocation2 = GetReferenceItem(10, expectedUri2);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));

            var virtualCSharpUri1 = new Uri("C:/path/to/file1.razor.g.cs");
            var virtualCSharpUri2 = new Uri("C:/path/to/file2.razor.g.cs");
            var csharpLocation1 = GetReferenceItem(100, virtualCSharpUri1);
            var csharpLocation2 = GetReferenceItem(200, virtualCSharpUri2);

            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);
            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            var token = Guid.NewGuid().ToString();
            var parameterToken = new JObject
            {
                { "token", token },
                { "value", JArray.FromObject(new[] { csharpLocation1, csharpLocation2 }) }
            };

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, VSReferenceItem[]>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, TextDocumentPositionParams, CancellationToken>((method, serverContentType, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentReferencesName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    lspFarEndpointCalled = true;

                    _ = lspProgressListener.ProcessProgressNotificationAsync(Methods.ProgressNotificationName, parameterToken);
                })
                .Returns(Task.FromResult(Array.Empty<VSReferenceItem>()));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult1 = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation1.Location.Range }
            };
            var remappingResult2 = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation2.Location.Range }
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider
                .Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, It.IsAny<Uri>(), It.IsAny<Range[]>(), It.IsAny<CancellationToken>()))
                .Returns<RazorLanguageKind, Uri, Range[], CancellationToken>((languageKind, uri, ranges, ct) => Task.FromResult(uri.LocalPath.Contains("file1") ? remappingResult1 : remappingResult2));

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object, lspProgressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                Assert.Collection(results,
                    a => AssertVSReferenceItem(expectedLocation1, a),
                    b => AssertVSReferenceItem(expectedLocation2, b));
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(lspFarEndpointCalled);
            Assert.True(progressReported);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_RemapsExternalRazorFiles()
        {
            // Arrange
            var progressReported = false;
            var externalUri = new Uri("C:/path/to/someotherfile.razor");
            var expectedLocation = GetReferenceItem(5, externalUri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 2, MockBehavior.Strict));
            documentManager.AddDocument(externalUri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 5, MockBehavior.Strict));

            var virtualCSharpUri = new Uri("C:/path/to/someotherfile.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, virtualCSharpUri);
            var (requestInvoker, progressListener) = MockServices(csharpLocation, out var token);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation.Location.Range },
                HostDocumentVersion = 5
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, externalUri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                var actualLocation = Assert.Single(results);
                AssertVSReferenceItem(expectedLocation, actualLocation);
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(progressReported);
        }

        [Theory]
        [InlineData("__o = food;", "food")]
        [InlineData("string Todo.<Title>k__BackingField", "string Todo.<Title>")]
        public async Task HandleRequestAsync_CSharpProjection_FiltersReferenceText(string rawText, string filteredText)
        {
            // Arrange
            var progressReported = false;
            var externalUri = new Uri("C:/path/to/someotherfile.razor");
            var expectedReferenceItem = GetReferenceItem(5, 5, 5, 5, externalUri, text: filteredText);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 2, MockBehavior.Strict));
            documentManager.AddDocument(externalUri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 5, MockBehavior.Strict));

            var virtualCSharpUri = new Uri("C:/path/to/someotherfile.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, 100, 100, 100, virtualCSharpUri, text: rawText);
            var (requestInvoker, progressListener) = MockServices(csharpLocation, out var token);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedReferenceItem.Location.Range },
                HostDocumentVersion = 5
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, externalUri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                var actualReferenceItem = Assert.Single(results);
                AssertVSReferenceItem(expectedReferenceItem, actualReferenceItem);
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(progressReported);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_FiltersReferenceClassifiedRuns()
        {
            // Arrange
            var progressReported = false;
            var externalUri = new Uri("C:/path/to/someotherfile.razor");

            var expectedClassifiedRun = new ClassifiedTextElement(new ClassifiedTextRun[]
            {
                new ClassifiedTextRun("text", "counter"),
            });
            var expectedReferenceItem = GetReferenceItem(5, 5, 5, 5, externalUri, text: expectedClassifiedRun);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 2, MockBehavior.Strict));
            documentManager.AddDocument(externalUri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 5, MockBehavior.Strict));

            var virtualClassifiedRun = new ClassifiedTextElement(new ClassifiedTextRun[]
            {
                new ClassifiedTextRun("field name", "__o"),
                new ClassifiedTextRun("text", " "),
                new ClassifiedTextRun("operator", "="),
                new ClassifiedTextRun("text", " "),
                new ClassifiedTextRun("text", "counter"),
                new ClassifiedTextRun("punctuation", ";"),
            });
            var virtualCSharpUri = new Uri("C:/path/to/someotherfile.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, 100, 100, 100, virtualCSharpUri, text: virtualClassifiedRun);
            var (requestInvoker, progressListener) = MockServices(csharpLocation, out var token);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedReferenceItem.Location.Range },
                HostDocumentVersion = 5
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, externalUri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                var actualReferenceItem = Assert.Single(results);
                AssertVSReferenceItem(expectedReferenceItem, actualReferenceItem);
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(progressReported);
        }

        [Fact]
        public async Task HandleRequestAsync_CSharpProjection_DoesNotRemapNonRazorFiles()
        {
            // Arrange
            var progressReported = false;
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));

            var externalCSharpUri = new Uri("C:/path/to/someotherfile.cs");
            var externalCsharpLocation = GetReferenceItem(100, externalCSharpUri);
            var (requestInvoker, progressListener) = MockServices(externalCsharpLocation, out var token);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = Mock.Of<LSPDocumentMappingProvider>(MockBehavior.Strict);
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                var actualLocation = Assert.Single(results);
                AssertVSReferenceItem(externalCsharpLocation, actualLocation);
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(progressReported);
        }

        [Fact]
        public async Task HandleRequestAsync_VersionMismatch_DiscardsLocation()
        {
            // Arrange
            var progressReported = false;
            var expectedLocation = GetReferenceItem(5, Uri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 123, MockBehavior.Strict));

            var virtualCSharpUri = new Uri("C:/path/to/file.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, virtualCSharpUri);
            var (requestInvoker, progressListener) = MockServices(csharpLocation, out var token);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation.Location.Range },
                HostDocumentVersion = 122 // Different from document version (123)
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, Uri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                Assert.Empty(results);
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(progressReported);
            Assert.Empty(result);
        }

        [Fact]
        public async Task HandleRequestAsync_VersionMismatch_DiscardsExternalRazorFiles()
        {
            // Arrange
            var progressReported = false;
            var externalUri = new Uri("C:/path/to/someotherfile.razor");
            var expectedLocation = GetReferenceItem(5, externalUri);
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 2, MockBehavior.Strict));
            documentManager.AddDocument(externalUri, Mock.Of<LSPDocumentSnapshot>(d => d.Version == 5, MockBehavior.Strict));

            var virtualCSharpUri = new Uri("C:/path/to/someotherfile.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, virtualCSharpUri);
            var (requestInvoker, progressListener) = MockServices(csharpLocation, out var token);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var remappingResult = new RazorMapToDocumentRangesResponse()
            {
                Ranges = new[] { expectedLocation.Location.Range },
                HostDocumentVersion = 6
            };
            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, externalUri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult(remappingResult));

            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                Assert.Empty(results);
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(progressReported);
            Assert.Empty(result);
        }

        [Fact]
        public async Task HandleRequestAsync_RemapFailure_DiscardsLocation()
        {
            // Arrange
            var progressReported = false;
            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));

            var virtualCSharpUri = new Uri("C:/path/to/file.razor.g.cs");
            var csharpLocation = GetReferenceItem(100, virtualCSharpUri);
            var (requestInvoker, progressListener) = MockServices(csharpLocation, out var token);

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider.Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, Uri, new[] { csharpLocation.Location.Range }, It.IsAny<CancellationToken>())).
                Returns(Task.FromResult<RazorMapToDocumentRangesResponse>(null));

            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker, documentManager, projectionProvider.Object, documentMappingProvider.Object, progressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                Assert.Empty(results);
                progressReported = true;
                completedTokenSource.CancelAfter(0);
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(progressReported);
            Assert.Empty(result);
        }

        [Fact]
        public async Task HandleRequestAsync_LargeProject_InvokesCSharpLanguageServer()
        {
            // Validates batching mechanism for the progress notification on large projects

            // Arrange
            var lspFarEndpointCalled = false;

            const int BATCH_SIZE = 10;
            const int NUM_BATCHES = 10;
            const int NUM_DOCUMENTS = BATCH_SIZE * NUM_BATCHES;
            const int MAPPING_OFFSET = 10;

            var expectedUris = new Uri[NUM_DOCUMENTS];
            var virtualUris = new Uri[NUM_DOCUMENTS];
            var expectedReferences = new VSReferenceItem[NUM_BATCHES][];
            var csharpUnmappedReferences = new VSReferenceItem[NUM_BATCHES][];
            var parameterTokens = new JObject[NUM_BATCHES];

            var token = Guid.NewGuid().ToString();

            var documentNumber = 0;
            for (var batch = 0; batch < NUM_BATCHES; ++batch)
            {
                expectedReferences[batch] = new VSReferenceItem[BATCH_SIZE];
                csharpUnmappedReferences[batch] = new VSReferenceItem[BATCH_SIZE];

                for (var documentInBatch = 0; documentInBatch < BATCH_SIZE; ++documentInBatch)
                {
                    expectedUris[documentNumber] = new Uri($"C:/path/to/file{documentNumber}.razor");
                    virtualUris[documentNumber] = new Uri($"C:/path/to/file{documentNumber}.razor.g.cs");
                    expectedReferences[batch][documentInBatch] = GetReferenceItem(documentNumber, expectedUris[documentNumber]);

                    var umappedOffset = documentNumber * MAPPING_OFFSET;
                    csharpUnmappedReferences[batch][documentInBatch] = GetReferenceItem(umappedOffset, virtualUris[documentNumber]);
                    documentNumber++;
                }

                parameterTokens[batch] = new JObject
                {
                    { "token", token },
                    { "value", JArray.FromObject(csharpUnmappedReferences[batch]) }
                };
            }

            var documentManager = new TestDocumentManager();
            documentManager.AddDocument(Uri, Mock.Of<LSPDocumentSnapshot>(MockBehavior.Strict));

            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);
            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);
            requestInvoker
                .Setup(r => r.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, VSReferenceItem[]>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, TextDocumentPositionParams, CancellationToken>((method, serverContentType, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentReferencesName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);
                    lspFarEndpointCalled = true;

                    for (var i = 0; i < NUM_BATCHES; ++i)
                    {
                        _ = lspProgressListener.ProcessProgressNotificationAsync(Methods.ProgressNotificationName, parameterTokens[i]);
                    }
                })
                .Returns(Task.FromResult(Array.Empty<VSReferenceItem>()));

            var projectionResult = new ProjectionResult()
            {
                LanguageKind = RazorLanguageKind.CSharp,
            };
            var projectionProvider = new Mock<LSPProjectionProvider>(MockBehavior.Strict);
            projectionProvider.Setup(p => p.GetProjectionAsync(It.IsAny<LSPDocumentSnapshot>(), It.IsAny<Position>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(projectionResult));

            var documentMappingProvider = new Mock<LSPDocumentMappingProvider>(MockBehavior.Strict);
            documentMappingProvider
                .Setup(d => d.MapToDocumentRangesAsync(RazorLanguageKind.CSharp, It.IsAny<Uri>(), It.IsAny<Range[]>(), It.IsAny<CancellationToken>()))
                .Returns<RazorLanguageKind, Uri, Range[], CancellationToken>((languageKind, uri, ranges, ct) =>
                {
                    var unmappedPosition = ranges[0].Start.Line;
                    var mappedPosition = unmappedPosition / MAPPING_OFFSET;

                    var mappedRange = new Range()
                    {
                        Start = new Position(mappedPosition, mappedPosition),
                        End = new Position(mappedPosition, mappedPosition)
                    };

                    var response = new RazorMapToDocumentRangesResponse()
                    {
                        Ranges = new[] { mappedRange }
                    };

                    return Task.FromResult(response);
                });

            using var completedTokenSource = new CancellationTokenSource();
            var referencesHandler = new FindAllReferencesHandler(requestInvoker.Object, documentManager, projectionProvider.Object, documentMappingProvider.Object, lspProgressListener);
            referencesHandler.GetTestAccessor().WaitForProgressNotificationTimeout = TestWaitForProgressNotificationTimeout;
            referencesHandler.GetTestAccessor().ImmediateNotificationTimeout = completedTokenSource.Token;

            var progressBatchesReported = new ConcurrentBag<VSReferenceItem[]>();
            var progressToken = new ProgressWithCompletion<object>((val) =>
            {
                var results = Assert.IsType<VSReferenceItem[]>(val);
                Assert.Equal(BATCH_SIZE, results.Length);
                progressBatchesReported.Add(results);
                if (progressBatchesReported.Count == NUM_BATCHES)
                {
                    // All expected results were received
                    completedTokenSource.CancelAfter(0);
                }
            });
            var referenceRequest = new ReferenceParams()
            {
                TextDocument = new TextDocumentIdentifier() { Uri = Uri },
                Position = new Position(10, 5),
                PartialResultToken = progressToken
            };

            // Act
            var result = await referencesHandler.HandleRequestAsync(referenceRequest, new ClientCapabilities(), token, CancellationToken.None).ConfigureAwait(false);

            // Assert
            Assert.True(lspFarEndpointCalled);

            var sortedBatchesReported = progressBatchesReported.ToList();
            sortedBatchesReported.Sort((VSReferenceItem[] a, VSReferenceItem[] b) =>
            {
                var indexA = a[0].Location.Range.Start.Character;
                var indexB = b[0].Location.Range.Start.Character;
                return indexA.CompareTo(indexB);
            });

            Assert.Equal(NUM_BATCHES, sortedBatchesReported.Count);

            for (var batch = 0; batch < NUM_BATCHES; ++batch)
            {
                for (var documentInBatch = 0; documentInBatch < BATCH_SIZE; ++documentInBatch)
                {
                    AssertVSReferenceItem(
                        expectedReferences[batch][documentInBatch],
                        sortedBatchesReported[batch][documentInBatch]);
                }
            }
        }

        private bool AssertVSReferenceItem(VSReferenceItem expected, VSReferenceItem actual)
        {
            Assert.Equal(expected.Location, actual.Location);
            Assert.Equal(expected.DisplayPath, actual.DisplayPath);

            if (actual.Text is string)
            {
                Assert.Equal(expected.Text, actual.Text);
                Assert.Equal(expected.DefinitionText, actual.DefinitionText);
            }
            else
            {
                Assert.Equal(
                    expected.Text as ClassifiedTextElement,
                    actual.Text as ClassifiedTextElement,
                    ClassifiedTextElementComparer.Default);
                Assert.Equal(
                    expected.DefinitionText as ClassifiedTextElement,
                    actual.DefinitionText as ClassifiedTextElement,
                    ClassifiedTextElementComparer.Default);
            }

            return true;
        }

        private (LSPRequestInvoker, LSPProgressListener) MockServices(VSReferenceItem csharpLocation, out string token)
        {
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);
            var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);
            var requestInvoker = new Mock<LSPRequestInvoker>(MockBehavior.Strict);

            token = Guid.NewGuid().ToString();
            var parameterToken = new JObject
            {
                { "token", token },
                { "value", JArray.FromObject(new[] { csharpLocation }) }
            };

            requestInvoker.Setup(i => i.ReinvokeRequestOnServerAsync<TextDocumentPositionParams, VSReferenceItem[]>(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TextDocumentPositionParams>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, TextDocumentPositionParams, CancellationToken>((method, serverContentType, definitionParams, ct) =>
                {
                    Assert.Equal(Methods.TextDocumentReferencesName, method);
                    Assert.Equal(RazorLSPConstants.CSharpContentTypeName, serverContentType);

                    _ = lspProgressListener.ProcessProgressNotificationAsync(Methods.ProgressNotificationName, parameterToken);
                })
                .Returns(Task.FromResult(Array.Empty<VSReferenceItem>()));

            return (requestInvoker.Object, lspProgressListener);
        }

        private VSReferenceItem GetReferenceItem(int position, Uri uri, string text = "text")
        {
            return GetReferenceItem(position, position, position, position, uri, text);
        }

        private VSReferenceItem GetReferenceItem(
            int startLine,
            int startCharacter,
            int endLine,
            int endCharacter,
            Uri uri,
            object text,
            string documentName = "document",
            string projectName = "project")
        {
            return new VSReferenceItem()
            {
                Location = new Location()
                {
                    Uri = uri,
                    Range = new Range()
                    {
                        Start = new Position(startLine, startCharacter),
                        End = new Position(endLine, endCharacter)
                    }
                },
                DocumentName = documentName,
                ProjectName = projectName,
                DisplayPath = uri.AbsolutePath,
                Text = text,
                DefinitionText = text
            };
        }
    }
}
