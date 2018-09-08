// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    // These tests are really integration tests. There isn't a good way to unit test this functionality since
    // the only thing in here is threading.
    public class BackgroundDocumentGeneratorTest : TestBase
    {
        public BackgroundDocumentGeneratorTest()
        {
            Documents = new HostDocument[]
            {
                new HostDocument("c:\\Test1\\Index.cshtml", "Index.cshtml"),
                new HostDocument("c:\\Test1\\Components\\Counter.cshtml", "Components\\Counter.cshtml"),
            };

            HostProject1 = new HostProject("c:\\Test1\\Test1.csproj", RazorConfiguration.Default);
            HostProject2 = new HostProject("c:\\Test2\\Test2.csproj", RazorConfiguration.Default);

            var projectId1 = ProjectId.CreateNewId("Test1");
            var projectId2 = ProjectId.CreateNewId("Test2");
        }

        private HostDocument[] Documents { get; }

        private HostProject HostProject1 { get; }

        private HostProject HostProject2 { get; }

        [ForegroundFact]
        public async Task Queue_ProcessesNotifications_AndGoesBackToSleep()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.HostProjectAdded(HostProject1);
            projectManager.HostProjectAdded(HostProject2);
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentAdded(HostProject1, Documents[1], null);

            var project = projectManager.GetLoadedProject(HostProject1.FilePath);

            var queue = new TestBackgroundDocumentGenerator(Dispatcher, Logger)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkStarting = new ManualResetEventSlim(initialState: false),
                BlockBackgroundWorkCompleting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false),
            };

            // Act & Assert
            queue.Enqueue(project.GetDocument(Documents[0].FilePath));

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();
            queue.BlockBackgroundWorkCompleting.Set();

            await Task.Run(() => queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)));

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }

        [ForegroundFact]
        public async Task Queue_ProcessesNotifications_AndRestarts()
        {
            // Arrange
            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.HostProjectAdded(HostProject1);
            projectManager.HostProjectAdded(HostProject2);
            projectManager.DocumentAdded(HostProject1, Documents[0], null);
            projectManager.DocumentAdded(HostProject1, Documents[1], null);

            var project = projectManager.GetLoadedProject(HostProject1.FilePath);

            var queue = new TestBackgroundDocumentGenerator(Dispatcher, Logger)
            {
                Delay = TimeSpan.FromMilliseconds(1),
                BlockBackgroundWorkStart = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkStarting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundCapturedWorkload = new ManualResetEventSlim(initialState: false),
                BlockBackgroundWorkCompleting = new ManualResetEventSlim(initialState: false),
                NotifyBackgroundWorkCompleted = new ManualResetEventSlim(initialState: false),
            };

            // Act & Assert
            queue.Enqueue(project.GetDocument(Documents[0].FilePath));

            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to start.
            queue.BlockBackgroundWorkStart.Set();

            await Task.Run(() => queue.NotifyBackgroundWorkStarting.Wait(TimeSpan.FromSeconds(1)));

            Assert.True(queue.IsScheduledOrRunning, "Worker should be processing now");

            await Task.Run(() => queue.NotifyBackgroundCapturedWorkload.Wait(TimeSpan.FromSeconds(1)));
            Assert.False(queue.HasPendingNotifications, "Worker should have taken all notifications");

            queue.Enqueue(project.GetDocument(Documents[1].FilePath));
            Assert.True(queue.HasPendingNotifications); // Now we should see the worker restart when it finishes.

            // Allow work to complete, which should restart the timer.
            queue.BlockBackgroundWorkCompleting.Set();

            await Task.Run(() => queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)));
            queue.NotifyBackgroundWorkCompleted.Reset();

            // It should start running again right away.
            Assert.True(queue.IsScheduledOrRunning, "Queue should be scheduled during Enqueue");
            Assert.True(queue.HasPendingNotifications, "Queue should have a notification created during Enqueue");

            // Allow the background work to proceed.
            queue.BlockBackgroundWorkStart.Set();

            queue.BlockBackgroundWorkCompleting.Set();
            await Task.Run(() => queue.NotifyBackgroundWorkCompleted.Wait(TimeSpan.FromSeconds(3)));

            Assert.False(queue.IsScheduledOrRunning, "Queue should not have restarted");
            Assert.False(queue.HasPendingNotifications, "Queue should have processed all notifications");
        }

        [Fact]
        public void ReportUnsynchronizableContent_DoesNothingForOldDocuments()
        {
            // Arrange
            var router = new TestRouter();
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, long>());
            var backgroundGenerator = new BackgroundDocumentGenerator(Dispatcher, cache, router, Logger);
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml");
            var work = new[] { new KeyValuePair<string, DocumentSnapshot>(document.FilePath, document) };

            // Act
            backgroundGenerator.ReportUnsynchronizableContent(work);

            // Assert
            Assert.Empty(router.SynchronizedDocuments);
        }

        [Fact]
        public void ReportUnsynchronizableContent_DoesNothingIfAlreadySynchronized()
        {
            // Arrange
            var router = new TestRouter();
            var document = TestDocumentSnapshot.Create("C:/path/file.cshtml", VersionStamp.Default.GetNewerVersion());
            var cache = new TestDocumentVersionCache(new Dictionary<DocumentSnapshot, long>()
            {
                [document] = 1337,
            });
            var csharpDocument = RazorCSharpDocument.Create("Anything", RazorCodeGenerationOptions.CreateDefault(), Enumerable.Empty<RazorDiagnostic>());

            // Force the state to already be up-to-date
            document.State.HostDocument.GeneratedCodeContainer.SetOutput(csharpDocument, document);

            var backgroundGenerator = new BackgroundDocumentGenerator(Dispatcher, cache, router, Logger);
            var work = new[] { new KeyValuePair<string, DocumentSnapshot>(document.FilePath, document) };

            // Act
            backgroundGenerator.ReportUnsynchronizableContent(work);

            // Assert
            Assert.Empty(router.SynchronizedDocuments);
        }

        [Fact]
        public void ReportUnsynchronizableContent_DoesNothingForOlderDocuments()
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
            oldDocument.State.HostDocument.GeneratedCodeContainer.SetOutput(csharpDocument, lastDocument);

            var backgroundGenerator = new BackgroundDocumentGenerator(Dispatcher, cache, router, Logger);
            var work = new[] { new KeyValuePair<string, DocumentSnapshot>(oldDocument.FilePath, oldDocument) };

            // Act
            backgroundGenerator.ReportUnsynchronizableContent(work);

            // Assert
            Assert.Empty(router.SynchronizedDocuments);
        }

        [Fact]
        public void ReportUnsynchronizableContent_DoesNothingIfSourceVersionsAreDifferent()
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
            document.State.HostDocument.GeneratedCodeContainer.SetOutput(csharpDocument, lastDocument);

            var backgroundGenerator = new BackgroundDocumentGenerator(Dispatcher, cache, router, Logger);
            var work = new[] { new KeyValuePair<string, DocumentSnapshot>(document.FilePath, document) };

            // Act
            backgroundGenerator.ReportUnsynchronizableContent(work);

            // Assert
            Assert.Empty(router.SynchronizedDocuments);
        }

        [Fact]
        public void ReportUnsynchronizableContent_SynchronizesIfSourceVersionsAreIdenticalButSyncVersionNewer()
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
            document.State.HostDocument.GeneratedCodeContainer.SetOutput(csharpDocument, lastDocument);

            var backgroundGenerator = new BackgroundDocumentGenerator(Dispatcher, cache, router, Logger);
            var work = new[] { new KeyValuePair<string, DocumentSnapshot>(document.FilePath, document) };

            // Act
            backgroundGenerator.ReportUnsynchronizableContent(work);

            // Assert
            var filePath = Assert.Single(router.SynchronizedDocuments);
            Assert.Equal(document.FilePath, filePath);
        }

        private class TestBackgroundDocumentGenerator : BackgroundDocumentGenerator
        {
            public TestBackgroundDocumentGenerator(ForegroundDispatcher foregroundDispatcher, VSCodeLogger logger) : base(foregroundDispatcher, logger)
            {
            }

            internal override void ReportUnsynchronizableContent(KeyValuePair<string, DocumentSnapshot>[] work)
            {
            }
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
