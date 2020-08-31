// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using DocumentUri = OmniSharp.Extensions.LanguageServer.Protocol.DocumentUri;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class DefaultWorkspaceDirectoryPathResolverTest
    {
        [Fact]
        public void Resolve_RootUriUnavailable_UsesRootPath()
        {
            // Arrange
            var expectedWorkspaceDirectory = "/testpath";
            var clientSettings = new InitializeParams()
            {
                RootPath = expectedWorkspaceDirectory
            };
            var server = Mock.Of<IClientLanguageServer>(server => server.ClientSettings == clientSettings);
            var workspaceDirectoryPathResolver = new DefaultWorkspaceDirectoryPathResolver(server);

            // Act
            var workspaceDirectoryPath = workspaceDirectoryPathResolver.Resolve();

            // Assert
            Assert.Equal(expectedWorkspaceDirectory, workspaceDirectoryPath);
        }

        [Fact]
        public void Resolve_RootUriPrefered()
        {
            // Arrange
            var initialWorkspaceDirectory = "\\\\testpath";
            var clientSettings = new InitializeParams()
            {
                RootPath = "/somethingelse",
                RootUri = new DocumentUri("file", authority: null, path: initialWorkspaceDirectory, query: null, fragment: null),
            };
            var server = Mock.Of<IClientLanguageServer>(server => server.ClientSettings == clientSettings);
            var workspaceDirectoryPathResolver = new DefaultWorkspaceDirectoryPathResolver(server);

            // Act
            var workspaceDirectoryPath = workspaceDirectoryPathResolver.Resolve();

            // Assert
            var expectedWorkspaceDirectory = "/"+initialWorkspaceDirectory;
            Assert.Equal(expectedWorkspaceDirectory, workspaceDirectoryPath);
        }
    }
}
