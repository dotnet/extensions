// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class UnsynchronizableContentDocumentProcessedListenerTest : LanguageServerTestBase
    {
        public UnsynchronizableContentDocumentProcessedListenerTest()
        {
            var projectSnapshotManager = new Mock<ProjectSnapshotManager>();
            projectSnapshotManager.Setup(psm => psm.IsDocumentOpen(It.IsAny<string>()))
                .Returns(true);
            ProjectSnapshotManager = projectSnapshotManager.Object;
        }

        private ProjectSnapshotManager ProjectSnapshotManager { get; }

        [Fact]
        public void DocumentProcessed_DoesNothingForOldDocuments()
        {
            // Arrange
            var router = new TestRouter();
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, long>());
            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, router);
            listener.Initialize(ProjectSnapshotManager);
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml");

            // Act
            listener.DocumentProcessed(document);

            // Assert
            Assert.Empty(router.SynchronizedDocuments);
        }

        [Fact]
        public void DocumentProcessed_DoesNothingIfAlreadySynchronized()
        {
            // Arrange
            var router = new TestRouter();
            var documentVersion = VersionStamp.Default.GetNewerVersion();
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml", documentVersion);
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, long>()
            {
                [document] = 1337,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());

            // Force the state to already be up-to-date
            document.State.HostDocument.GeneratedCodeContainer.SetOutput(document, csharpDocument, documentVersion.GetNewerVersion(), VersionStamp.Default);

            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, router);
            listener.Initialize(ProjectSnapshotManager);

            // Act
            listener.DocumentProcessed(document);

            // Assert
            Assert.Empty(router.SynchronizedDocuments);
        }

        [Fact]
        public void DocumentProcessed_DoesNothingForOlderDocuments()
        {
            // Arrange
            var router = new TestRouter();
            var lastVersion = VersionStamp.Default.GetNewerVersion();
            var lastDocument = TestDocumentSnapshot.Create("C:/path/old.cshtml", lastVersion);
            var oldDocument = TestDocumentSnapshot.Create("C:/path/file.cshtml", VersionStamp.Default);
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, long>()
            {
                [oldDocument] = 1337,
                [lastDocument] = 1338,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());

            // Force the state to already be up-to-date
            oldDocument.State.HostDocument.GeneratedCodeContainer.SetOutput(lastDocument, csharpDocument, lastVersion, VersionStamp.Default);

            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, router);
            listener.Initialize(ProjectSnapshotManager);

            // Act
            listener.DocumentProcessed(oldDocument);

            // Assert
            Assert.Empty(router.SynchronizedDocuments);
        }

        [Fact]
        public void DocumentProcessed_DoesNothingIfSourceVersionsAreDifferent()
        {
            // Arrange
            var router = new TestRouter();
            var lastVersion = VersionStamp.Default.GetNewerVersion();
            var lastDocument = TestDocumentSnapshot.Create("C:/path/old.cshtml", lastVersion);
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml", VersionStamp.Default);
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, long>()
            {
                [document] = 1338,
                [lastDocument] = 1337,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());

            // Force the state to already be up-to-date
            document.State.HostDocument.GeneratedCodeContainer.SetOutput(lastDocument, csharpDocument, lastVersion, VersionStamp.Default);

            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, router);
            listener.Initialize(ProjectSnapshotManager);

            // Act
            listener.DocumentProcessed(document);

            // Assert
            Assert.Empty(router.SynchronizedDocuments);
        }

        [Fact]
        public void DocumentProcessed_SynchronizesIfSourceVersionsAreIdenticalButSyncVersionNewer()
        {
            // Arrange
            var router = new TestRouter();
            var lastVersion = VersionStamp.Default.GetNewerVersion();
            var lastDocument = TestDocumentSnapshot.Create("C:/path/old.cshtml", lastVersion);
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml", lastVersion);
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, long>()
            {
                [document] = 1338,
                [lastDocument] = 1337,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());

            // Force the state to already be up-to-date
            document.State.HostDocument.GeneratedCodeContainer.SetOutput(lastDocument, csharpDocument, lastVersion, VersionStamp.Default);

            var listener = new UnsynchronizableContentDocumentProcessedListener(Dispatcher, cache, router);
            listener.Initialize(ProjectSnapshotManager);

            // Act
            listener.DocumentProcessed(document);

            // Assert
            var filePath = Assert.Single(router.SynchronizedDocuments);
            Assert.Equal(document.FilePath, filePath);
        }

        private class TestDocumentVersionCache : DocumentVersionCache
        {
            private readonly Dictionary<DocumentSnapshot, long> _versions;

            public TestDocumentVersionCache(Dictionary<DocumentSnapshot, long> versions)
            {
                if (versions == null)
                {
                    throw new ArgumentNullException(nameof(versions));
                }

                _versions = versions;
            }

            public override bool TryGetDocumentVersion(DocumentSnapshot documentSnapshot, out long version)
            {
                return _versions.TryGetValue(documentSnapshot, out version);
            }

            public override void TrackDocumentVersion(DocumentSnapshot documentSnapshot, long version) => throw new NotImplementedException();

            public override void Initialize(ProjectSnapshotManagerBase projectManager)
            {
                throw new NotImplementedException();
            }
        }

        private class TestRouter : ILanguageServer
        {
            public TestRouter()
            {
                var synchronizedDocuments = new List<string>();
                SynchronizedDocuments = synchronizedDocuments;
                Client = new TestClient(synchronizedDocuments);
            }

            public IReadOnlyList<string> SynchronizedDocuments { get; set; }

            public ILanguageServerClient Client { get; }

            public ILanguageServerDocument Document => throw new NotImplementedException();

            public ILanguageServerWindow Window => throw new NotImplementedException();

            public ILanguageServerWorkspace Workspace => throw new NotImplementedException();

            public IDisposable AddHandler(string method, IJsonRpcHandler handler) => throw new NotImplementedException();

            public IDisposable AddHandler(string method, Func<IServiceProvider, IJsonRpcHandler> handlerFunc) => throw new NotImplementedException();

            public IDisposable AddHandler<T>() where T : IJsonRpcHandler => throw new NotImplementedException();

            public IDisposable AddHandlers(params IJsonRpcHandler[] handlers) => throw new NotImplementedException();

            public IDisposable AddTextDocumentIdentifier(params ITextDocumentIdentifier[] handlers) => throw new NotImplementedException();

            public IDisposable AddTextDocumentIdentifier<T>() where T : ITextDocumentIdentifier => throw new NotImplementedException();

            public TaskCompletionSource<JToken> GetRequest(long id) => throw new NotImplementedException();

            public void SendNotification(string method) => throw new NotImplementedException();

            public void SendNotification<T>(string method, T @params) => throw new NotImplementedException();

            public Task<TResponse> SendRequest<T, TResponse>(string method, T @params) => throw new NotImplementedException();

            public Task<TResponse> SendRequest<TResponse>(string method) => throw new NotImplementedException();

            public Task SendRequest<T>(string method, T @params) => throw new NotImplementedException();

            private class TestClient : ILanguageServerClient
            {
                private readonly List<string> _synchronizedDocuments;

                public TestClient(List<string> synchronizedDocuments)
                {
                    if (synchronizedDocuments == null)
                    {
                        throw new ArgumentNullException(nameof(synchronizedDocuments));
                    }

                    _synchronizedDocuments = synchronizedDocuments;
                }

                public Task SendRequest<T>(string method, T @params)
                {
                    var updateRequest = @params as UpdateCSharpBufferRequest;

                    _synchronizedDocuments.Add(updateRequest.HostDocumentFilePath);

                    return Task.CompletedTask;
                }

                public TaskCompletionSource<JToken> GetRequest(long id) => throw new NotImplementedException();

                public void SendNotification(string method) => throw new NotImplementedException();

                public void SendNotification<T>(string method, T @params) => throw new NotImplementedException();

                public Task<TResponse> SendRequest<T, TResponse>(string method, T @params) => throw new NotImplementedException();

                public Task<TResponse> SendRequest<TResponse>(string method) => throw new NotImplementedException();
            }
        }
    }
}
