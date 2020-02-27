// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultGeneratedDocumentPublisherTest : LanguageServerTestBase
    {
        public DefaultGeneratedDocumentPublisherTest()
        {
            Server = new TestServer();
            ProjectManager = TestProjectSnapshotManager.Create(Dispatcher);
            ProjectManager.AllowNotifyListeners = true;
            HostProject = new HostProject("/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
            ProjectManager.ProjectAdded(HostProject);
            HostDocument = new HostDocument("/path/to/file.razor", "file.razor");
            ProjectManager.DocumentAdded(HostProject, HostDocument, new EmptyTextLoader(HostDocument.FilePath));
        }

        private TestServer Server { get; }

        private TestProjectSnapshotManager ProjectManager { get; }

        private HostProject HostProject { get; }

        private HostDocument HostDocument { get; }

        [Fact]
        public void PublishCSharp_FirstTime_PublishesEntireSourceText()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            var content = "// C# content";
            var sourceText = SourceText.From(content);

            // Act
            generatedDocumentPublisher.PublishCSharp("/path/to/file.razor", sourceText, 123);

            // Assert
            var updateRequest = Assert.Single(Server.UpdateRequests);
            Assert.Equal("/path/to/file.razor", updateRequest.HostDocumentFilePath);
            var textChange = Assert.Single(updateRequest.Changes);
            Assert.Equal(content, textChange.NewText);
            Assert.Equal(123, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void PublishHtml_FirstTime_PublishesEntireSourceText()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            var content = "HTML content";
            var sourceText = SourceText.From(content);

            // Act
            generatedDocumentPublisher.PublishHtml("/path/to/file.razor", sourceText, 123);

            // Assert
            var updateRequest = Assert.Single(Server.UpdateRequests);
            Assert.Equal("/path/to/file.razor", updateRequest.HostDocumentFilePath);
            var textChange = Assert.Single(updateRequest.Changes);
            Assert.Equal(content, textChange.NewText);
            Assert.Equal(123, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void PublishCSharp_SecondTime_PublishesSourceTextDifferences()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            var initialSourceText = SourceText.From("// Initial content");
            generatedDocumentPublisher.PublishCSharp("/path/to/file.razor", initialSourceText, 123);
            var change = new TextChange(
                new TextSpan(initialSourceText.Length, 0),
                "!!");
            var changedSourceText = initialSourceText.WithChanges(change);

            // Act
            generatedDocumentPublisher.PublishCSharp("/path/to/file.razor", changedSourceText, 124);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal("/path/to/file.razor", updateRequest.HostDocumentFilePath);
            var textChange = Assert.Single(updateRequest.Changes);
            Assert.Equal(change, textChange);
            Assert.Equal(124, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void PublishHtml_SecondTime_PublishesSourceTextDifferences()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            var initialSourceText = SourceText.From("HTML content");
            generatedDocumentPublisher.PublishHtml("/path/to/file.razor", initialSourceText, 123);
            var change = new TextChange(
                new TextSpan(initialSourceText.Length, 0),
                "!!");
            var changedSourceText = initialSourceText.WithChanges(change);

            // Act
            generatedDocumentPublisher.PublishHtml("/path/to/file.razor", changedSourceText, 124);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal("/path/to/file.razor", updateRequest.HostDocumentFilePath);
            var textChange = Assert.Single(updateRequest.Changes);
            Assert.Equal(change, textChange);
            Assert.Equal(124, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void PublishCSharp_SecondTime_IdenticalContent_NoTextChanges()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            var sourceTextContent = "// The content";
            var initialSourceText = SourceText.From(sourceTextContent);
            generatedDocumentPublisher.PublishCSharp("/path/to/file.razor", initialSourceText, 123);
            var identicalSourceText = SourceText.From(sourceTextContent);

            // Act
            generatedDocumentPublisher.PublishCSharp("/path/to/file.razor", identicalSourceText, 124);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal("/path/to/file.razor", updateRequest.HostDocumentFilePath);
            Assert.Empty(updateRequest.Changes);
            Assert.Equal(124, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void PublishHtml_SecondTime_IdenticalContent_NoTextChanges()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            var sourceTextContent = "HTMl content";
            var initialSourceText = SourceText.From(sourceTextContent);
            generatedDocumentPublisher.PublishHtml("/path/to/file.razor", initialSourceText, 123);
            var identicalSourceText = SourceText.From(sourceTextContent);

            // Act
            generatedDocumentPublisher.PublishHtml("/path/to/file.razor", identicalSourceText, 124);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal("/path/to/file.razor", updateRequest.HostDocumentFilePath);
            Assert.Empty(updateRequest.Changes);
            Assert.Equal(124, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void PublishCSharp_DifferentFileSameContent_PublishesEverything()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            var sourceTextContent = "// The content";
            var initialSourceText = SourceText.From(sourceTextContent);
            generatedDocumentPublisher.PublishCSharp("/path/to/file1.razor", initialSourceText, 123);
            var identicalSourceText = SourceText.From(sourceTextContent);

            // Act
            generatedDocumentPublisher.PublishCSharp("/path/to/file2.razor", identicalSourceText, 123);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal("/path/to/file2.razor", updateRequest.HostDocumentFilePath);
            var textChange = Assert.Single(updateRequest.Changes);
            Assert.Equal(sourceTextContent, textChange.NewText);
            Assert.Equal(123, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void PublishHtml_DifferentFileSameContent_PublishesEverything()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            var sourceTextContent = "HTML content";
            var initialSourceText = SourceText.From(sourceTextContent);
            generatedDocumentPublisher.PublishHtml("/path/to/file1.razor", initialSourceText, 123);
            var identicalSourceText = SourceText.From(sourceTextContent);

            // Act
            generatedDocumentPublisher.PublishHtml("/path/to/file2.razor", identicalSourceText, 123);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal("/path/to/file2.razor", updateRequest.HostDocumentFilePath);
            var textChange = Assert.Single(updateRequest.Changes);
            Assert.Equal(sourceTextContent, textChange.NewText);
            Assert.Equal(123, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void ProjectSnapshotManager_DocumentChanged_OpenDocument_PublishesEmptyTextChanges()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            generatedDocumentPublisher.Initialize(ProjectManager);
            var sourceTextContent = "// The content";
            var initialSourceText = SourceText.From(sourceTextContent);
            generatedDocumentPublisher.PublishCSharp(HostDocument.FilePath, initialSourceText, 123);

            // Act
            ProjectManager.DocumentOpened(HostProject.FilePath, HostDocument.FilePath, initialSourceText);
            generatedDocumentPublisher.PublishCSharp(HostDocument.FilePath, initialSourceText, 123);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal(HostDocument.FilePath, updateRequest.HostDocumentFilePath);
            Assert.Empty(updateRequest.Changes);
            Assert.Equal(123, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void ProjectSnapshotManager_DocumentChanged_ClosedDocument_RepublishesTextChanges()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            generatedDocumentPublisher.Initialize(ProjectManager);
            var sourceTextContent = "// The content";
            var initialSourceText = SourceText.From(sourceTextContent);
            generatedDocumentPublisher.PublishCSharp(HostDocument.FilePath, initialSourceText, 123);
            ProjectManager.DocumentOpened(HostProject.FilePath, HostDocument.FilePath, initialSourceText);

            // Act
            ProjectManager.DocumentClosed(HostProject.FilePath, HostDocument.FilePath, new EmptyTextLoader(HostDocument.FilePath));
            generatedDocumentPublisher.PublishCSharp(HostDocument.FilePath, initialSourceText, 123);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal(HostDocument.FilePath, updateRequest.HostDocumentFilePath);
            var textChange = Assert.Single(updateRequest.Changes);
            Assert.Equal(sourceTextContent, textChange.NewText);
            Assert.Equal(123, updateRequest.HostDocumentVersion);
        }

        [Fact]
        public void ProjectSnapshotManager_DocumentRemoved_RepublishesTextChanges()
        {
            // Arrange
            var generatedDocumentPublisher = new DefaultGeneratedDocumentPublisher(Dispatcher, new Lazy<ILanguageServer>(() => Server));
            generatedDocumentPublisher.Initialize(ProjectManager);
            var sourceTextContent = "// The content";
            var initialSourceText = SourceText.From(sourceTextContent);
            generatedDocumentPublisher.PublishCSharp(HostDocument.FilePath, initialSourceText, 123);

            // Act
            ProjectManager.DocumentRemoved(HostProject, HostDocument);
            generatedDocumentPublisher.PublishCSharp(HostDocument.FilePath, initialSourceText, 123);

            // Assert
            Assert.Equal(2, Server.UpdateRequests.Count);
            var updateRequest = Server.UpdateRequests.Last();
            Assert.Equal(HostDocument.FilePath, updateRequest.HostDocumentFilePath);
            var textChange = Assert.Single(updateRequest.Changes);
            Assert.Equal(sourceTextContent, textChange.NewText);
            Assert.Equal(123, updateRequest.HostDocumentVersion);
        }

        private class TestServer : ILanguageServer
        {
            public TestServer()
            {
                var synchronizedDocuments = new List<UpdateBufferRequest>();
                UpdateRequests = synchronizedDocuments;
                Client = new TestClient(synchronizedDocuments);
            }

            public IReadOnlyList<UpdateBufferRequest> UpdateRequests { get; }

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
                private readonly List<UpdateBufferRequest> _updateRequests;

                public TestClient(List<UpdateBufferRequest> updateRequests)
                {
                    if (updateRequests == null)
                    {
                        throw new ArgumentNullException(nameof(updateRequests));
                    }

                    _updateRequests = updateRequests;
                }

                public Task SendRequest<T>(string method, T @params)
                {
                    var updateRequest = @params as UpdateBufferRequest;

                    _updateRequests.Add(updateRequest);

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
