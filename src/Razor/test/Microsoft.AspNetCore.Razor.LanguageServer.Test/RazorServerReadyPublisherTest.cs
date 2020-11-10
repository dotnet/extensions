// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Moq;
using OmniSharp.Extensions.JsonRpc;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public class RazorServerReadyPublisherTest : LanguageServerTestBase
    {
        [Fact]
        public void ProjectSnapshotManager_WorkspaceNull_DoesNothing()
        {
            // Arrange
            var clientNotifierService = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);

            var razorServerReadyPublisher = new RazorServerReadyPublisher(Dispatcher, clientNotifierService.Object);

            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.AllowNotifyListeners = true;

            razorServerReadyPublisher.Initialize(projectManager);

            var document = TestDocumentSnapshot.Create("C:/file.cshtml");
            document.TryGetText(out var text);
            document.TryGetTextVersion(out var textVersion);
            var textAndVersion = TextAndVersion.Create(text, textVersion);

            // Act
            projectManager.ProjectAdded(document.ProjectInternal.HostProject);
            projectManager.DocumentAdded(document.ProjectInternal.HostProject, document.State.HostDocument, TextLoader.From(textAndVersion));

            // Assert
            // Should not have been called
            clientNotifierService.Verify();
        }

        private const string _razorServerReadyEndpoint = "razor/serverReady";

        [Fact]
        public void ProjectSnapshotManager_WorkspacePopulated_SetsUIContext()
        {
            // Arrange
            var responseRouterReturns = new Mock<IResponseRouterReturns>(MockBehavior.Strict);
            responseRouterReturns.Setup(r => r.ReturningVoid(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            var clientNotifierService = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            clientNotifierService.Setup(l => l.SendRequestAsync(_razorServerReadyEndpoint))
                .Returns(Task.FromResult(responseRouterReturns.Object));

            var razorServerReadyPublisher = new RazorServerReadyPublisher(Dispatcher, clientNotifierService.Object);

            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.AllowNotifyListeners = true;

            razorServerReadyPublisher.Initialize(projectManager);

            var document = TestDocumentSnapshot.Create("C:/file.cshtml");
            document.TryGetText(out var text);
            document.TryGetTextVersion(out var textVersion);
            var textAndVersion = TextAndVersion.Create(text, textVersion);

            // Act
            projectManager.ProjectAdded(document.ProjectInternal.HostProject);
            projectManager.ProjectWorkspaceStateChanged(document.ProjectInternal.HostProject.FilePath, CreateProjectWorkspace());

            // Assert
            clientNotifierService.VerifyAll();
            responseRouterReturns.VerifyAll();
        }

        [Fact]
        public void ProjectSnapshotManager_WorkspacePopulated_DoesNotFireTwice()
        {
            // Arrange
            var responseRouterReturns = new Mock<IResponseRouterReturns>(MockBehavior.Strict);
            responseRouterReturns.Setup(r => r.ReturningVoid(It.IsAny<CancellationToken>()))
                .Returns(() => Task.CompletedTask);

            var clientNotifierService = new Mock<ClientNotifierServiceBase>(MockBehavior.Strict);
            clientNotifierService.Setup(l => l.SendRequestAsync(_razorServerReadyEndpoint))
                .Returns(Task.FromResult(responseRouterReturns.Object));

            var razorServerReadyPublisher = new RazorServerReadyPublisher(Dispatcher, clientNotifierService.Object);

            var projectManager = TestProjectSnapshotManager.Create(Dispatcher);
            projectManager.AllowNotifyListeners = true;

            razorServerReadyPublisher.Initialize(projectManager);
            var document = TestDocumentSnapshot.Create("C:/file.cshtml");
            document.TryGetText(out var text);
            document.TryGetTextVersion(out var textVersion);
            var textAndVersion = TextAndVersion.Create(text, textVersion);

            projectManager.ProjectAdded(document.ProjectInternal.HostProject);

            // Act
            projectManager.ProjectWorkspaceStateChanged(document.ProjectInternal.HostProject.FilePath, CreateProjectWorkspace());

            clientNotifierService.VerifyAll();
            responseRouterReturns.VerifyAll();

            projectManager.ProjectWorkspaceStateChanged(document.ProjectInternal.HostProject.FilePath, CreateProjectWorkspace());

            // Assert
            clientNotifierService.VerifyAll();
            responseRouterReturns.Verify(r => r.ReturningVoid(It.IsAny<CancellationToken>()), Times.Once);
        }

        private ProjectWorkspaceState CreateProjectWorkspace()
        {
            var tagHelpers = new List<TagHelperDescriptor>();

            return new ProjectWorkspaceState(tagHelpers, default);
        }
    }
}
