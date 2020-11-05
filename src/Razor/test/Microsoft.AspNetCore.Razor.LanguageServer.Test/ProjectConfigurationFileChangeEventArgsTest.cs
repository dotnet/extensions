// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.AspNetCore.Razor.LanguageServer.Serialization;
using Microsoft.CodeAnalysis.Razor.Workspaces.Serialization;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class ProjectConfigurationFileChangeEventArgsTest
    {
        [Fact]
        public void TryDeserialize_RemovedKind_ReturnsFalse()
        {
            // Arrange
            var jsonFileDeserializer = new Mock<JsonFileDeserializer>();
            jsonFileDeserializer.Setup(deserializer => deserializer.Deserialize<FullProjectSnapshotHandle>(It.IsAny<string>()))
                .Returns(new FullProjectSnapshotHandle("c:/path/to/project.csproj", configuration: null, rootNamespace: null, projectWorkspaceState: null, documents: Array.Empty<DocumentSnapshotHandle>()));
            var args = new ProjectConfigurationFileChangeEventArgs("c:/some/path", RazorFileChangeKind.Removed, jsonFileDeserializer.Object);

            // Act
            var result = args.TryDeserialize(out var handle);

            // Assert
            Assert.False(result);
            Assert.Null(handle);
        }

        [Fact]
        public void TryDeserialize_MemoizesResults()
        {
            // Arrange
            var jsonFileDeserializer = new Mock<JsonFileDeserializer>();
            var projectSnapshotHandle = new FullProjectSnapshotHandle("c:/path/to/project.csproj", configuration: null, rootNamespace: null, projectWorkspaceState: null, documents: Array.Empty<DocumentSnapshotHandle>());
            jsonFileDeserializer.Setup(deserializer => deserializer.Deserialize<FullProjectSnapshotHandle>(It.IsAny<string>()))
                .Returns(projectSnapshotHandle);
            var args = new ProjectConfigurationFileChangeEventArgs("c:/some/path", RazorFileChangeKind.Added, jsonFileDeserializer.Object);

            // Act
            var result1 = args.TryDeserialize(out var handle1);
            var result2 = args.TryDeserialize(out var handle2);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.Same(projectSnapshotHandle, handle1);
            Assert.Same(projectSnapshotHandle, handle2);
        }

        [Fact]
        public void TryDeserialize_NullFileDeserialization_MemoizesResults_ReturnsFalse()
        {
            // Arrange
            var jsonFileDeserializer = new Mock<JsonFileDeserializer>();
            var callCount = 0;
            jsonFileDeserializer.Setup(deserializer => deserializer.Deserialize<FullProjectSnapshotHandle>(It.IsAny<string>()))
                .Callback(() => callCount++)
                .Returns<FullProjectSnapshotHandle>(null);
            var args = new ProjectConfigurationFileChangeEventArgs("c:/some/path", RazorFileChangeKind.Changed, jsonFileDeserializer.Object);

            // Act
            var result1 = args.TryDeserialize(out var handle1);
            var result2 = args.TryDeserialize(out var handle2);

            // Assert
            Assert.False(result1);
            Assert.False(result2);
            Assert.Null(handle1);
            Assert.Null(handle2);
            Assert.Equal(1, callCount);
        }
    }
}
