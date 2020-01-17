// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Server;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorFileChangeDetectorManagerTest
    {
        [Fact]
        public void ResolveWorkspaceDirectory_RootUriUnavailable_UsesRootPath()
        {
            // Arrange
            var expectedWorkspaceDirectory = "/testpath";
            var clientSettings = new InitializeParams()
            {
                RootPath = expectedWorkspaceDirectory
            };

            // Act
            var workspaceDirectory = RazorFileChangeDetectorManager.ResolveWorkspaceDirectory(clientSettings);

            // Assert
            Assert.Equal(expectedWorkspaceDirectory, workspaceDirectory);
        }

        [Fact]
        public void ResolveWorkspaceDirectory_RootUriPrefered()
        {
            // Arrange
            var expectedWorkspaceDirectory = "\\\\testpath";
            var clientSettings = new InitializeParams()
            {
                RootPath = "/somethingelse",
                RootUri = new Uri(expectedWorkspaceDirectory),
            };

            // Act
            var workspaceDirectory = RazorFileChangeDetectorManager.ResolveWorkspaceDirectory(clientSettings);

            // Assert
            Assert.Equal(expectedWorkspaceDirectory, workspaceDirectory);
        }

        [Fact]
        public async Task InitializedAsync_StartsFileChangeDetectors()
        {
            // Arrange
            var expectedWorkspaceDirectory = "\\\\testpath";
            var clientSettings = new InitializeParams()
            {
                RootUri = new Uri(expectedWorkspaceDirectory),
            };
            var languageServer = Mock.Of<ILanguageServer>(server => server.ClientSettings == clientSettings);
            var detector1 = new Mock<IFileChangeDetector>(MockBehavior.Strict);
            detector1.Setup(detector => detector.StartAsync(expectedWorkspaceDirectory, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var detector2 = new Mock<IFileChangeDetector>(MockBehavior.Strict);
            detector2.Setup(detector => detector.StartAsync(expectedWorkspaceDirectory, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();
            var detectorManager = new RazorFileChangeDetectorManager(new[] { detector1.Object, detector2.Object });

            // Act
            await detectorManager.InitializedAsync(languageServer);

            // Assert
            detector1.VerifyAll();
            detector2.VerifyAll();
        }
    }
}
