// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorFileChangeDetectorManagerTest
    {
        [Fact]
        public async Task InitializedAsync_StartsFileChangeDetectors()
        {
            // Arrange
            var initialWorkspaceDirectory = "\\\\testpath";

            var clientSettings = new InitializeParams()
            {
                RootUri = new DocumentUri("file", authority: null, path: initialWorkspaceDirectory, query: null, fragment: null),
            };
            var languageServer = new Mock<IClientLanguageServer>(MockBehavior.Strict);
            languageServer.SetupGet(s => s.ClientSettings)
                .Returns(clientSettings);
            var detector1 = new Mock<IFileChangeDetector>(MockBehavior.Strict);
            var expectedWorkspaceDirectory = "/" + initialWorkspaceDirectory;
            detector1.Setup(detector => detector.StartAsync(expectedWorkspaceDirectory, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var detector2 = new Mock<IFileChangeDetector>(MockBehavior.Strict);
            detector2.Setup(detector => detector.StartAsync(expectedWorkspaceDirectory, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var workspaceDirectoryPathResolver = new DefaultWorkspaceDirectoryPathResolver(languageServer.Object);
            var detectorManager = new RazorFileChangeDetectorManager(workspaceDirectoryPathResolver, new[] { detector1.Object, detector2.Object });

            // Act
            await detectorManager.InitializedAsync();

            // Assert
            detector1.VerifyAll();
            detector2.VerifyAll();
            languageServer.VerifyAll();
        }

        [Fact]
        public async Task InitializedAsync_Disposed_ReStopsFileChangeDetectors()
        {
            // Arrange
            var expectedWorkspaceDirectory = "\\\\testpath";
            var clientSettings = new InitializeParams()
            {
                RootUri = new DocumentUri("file", authority: null, path: expectedWorkspaceDirectory, query: null, fragment: null),
            };
            var languageServer = new Mock<IClientLanguageServer>(MockBehavior.Strict);
            languageServer
                .SetupGet(s => s.ClientSettings)
                .Returns(clientSettings);

            var detector = new Mock<IFileChangeDetector>(MockBehavior.Strict);
            var cts = new TaskCompletionSource<bool>();
            detector.Setup(d => d.StartAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(cts.Task);
            var stopCount = 0;
            detector.Setup(d => d.Stop()).Callback(() => stopCount++);
            var workspaceDirectoryPathResolver = new DefaultWorkspaceDirectoryPathResolver(languageServer.Object);
            var detectorManager = new RazorFileChangeDetectorManager(workspaceDirectoryPathResolver, new[] { detector.Object });

            // Act
            var initializeTask = detectorManager.InitializedAsync();
            detectorManager.Dispose();

            // Unblock the detector start
            cts.SetResult(true);
            await initializeTask;

            // Assert
            Assert.Equal(2, stopCount);

            languageServer.VerifyAll();
        }
    }
}
