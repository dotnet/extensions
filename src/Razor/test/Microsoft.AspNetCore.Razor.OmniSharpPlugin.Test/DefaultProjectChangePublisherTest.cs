// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class DefaultProjectChangePublisherTest : OmniSharpTestBase
    {
        [Theory]
        [InlineData(OmniSharpProjectChangeKind.DocumentAdded)]
        [InlineData(OmniSharpProjectChangeKind.DocumentRemoved)]
        [InlineData(OmniSharpProjectChangeKind.ProjectChanged)]
        public async Task ProjectManager_Changed_EnqueuesPublish(OmniSharpProjectChangeKind changeKind)
        {
            // Arrange
            var serializationSuccessful = false;
            var projectSnapshot = CreateProjectSnapshot("/path/to/project.csproj");
            var expectedPublishFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestProjectChangePublisher(
                LoggerFactory,
                onSerializeToFile: (snapshot, publishFilePath) =>
                {
                    Assert.Same(projectSnapshot, snapshot);
                    Assert.Equal(expectedPublishFilePath, publishFilePath);
                    serializationSuccessful = true;
                })
            {
                EnqueueDelay = 10
            };
            publisher.SetPublishFilePath(projectSnapshot.FilePath, expectedPublishFilePath);
            var args = OmniSharpProjectChangeEventArgs.CreateTestInstance(projectSnapshot, projectSnapshot, documentFilePath: null, changeKind);

            // Act
            publisher.ProjectManager_Changed(null, args);

            // Assert
            var kvp = Assert.Single(publisher._deferredPublishTasks);
            await kvp.Value;
            Assert.True(serializationSuccessful);
        }

        [Fact]
        public async Task ProjectManager_Changed_ProjectRemoved_AfterEnqueuedPublish()
        {
            // Arrange
            var attemptedToSerialize = false;
            var projectSnapshot = CreateProjectSnapshot("/path/to/project.csproj");
            var expectedPublishFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestProjectChangePublisher(
                LoggerFactory,
                onSerializeToFile: (snapshot, publishFilePath) => attemptedToSerialize = true)
            {
                EnqueueDelay = 10
            };
            publisher.SetPublishFilePath(projectSnapshot.FilePath, expectedPublishFilePath);
            publisher.EnqueuePublish(projectSnapshot);
            var args = OmniSharpProjectChangeEventArgs.CreateTestInstance(projectSnapshot, newer: null, documentFilePath: null, OmniSharpProjectChangeKind.ProjectRemoved);

            // Act
            publisher.ProjectManager_Changed(null, args);

            // Assert
            await Task.Delay(publisher.EnqueueDelay * 3);

            Assert.False(attemptedToSerialize);
        }

        [Fact]
        public async Task EnqueuePublish_BatchesPublishRequests()
        {
            // Arrange
            var serializationSuccessful = false;
            var firstSnapshot = CreateProjectSnapshot("/path/to/project.csproj");
            var secondSnapshot = CreateProjectSnapshot("/path/to/project.csproj", new[] { "/path/to/file.cshtml" });
            var expectedPublishFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestProjectChangePublisher(
                LoggerFactory,
                onSerializeToFile: (snapshot, publishFilePath) =>
                {
                    Assert.Same(secondSnapshot, snapshot);
                    Assert.Equal(expectedPublishFilePath, publishFilePath);
                    serializationSuccessful = true;
                })
            {
                EnqueueDelay = 10
            };
            publisher.SetPublishFilePath(firstSnapshot.FilePath, expectedPublishFilePath);

            // Act
            publisher.EnqueuePublish(firstSnapshot);
            publisher.EnqueuePublish(secondSnapshot);

            // Assert
            var kvp = Assert.Single(publisher._deferredPublishTasks);
            await kvp.Value;
            Assert.True(serializationSuccessful);
        }

        [Fact]
        public void Publish_UnsetPublishFilePath_Noops()
        {
            // Arrange
            var publisher = new TestProjectChangePublisher(LoggerFactory);
            var omniSharpProjectSnapshot = CreateProjectSnapshot("/path/to/project.csproj");

            // Act & Assert
            publisher.Publish(omniSharpProjectSnapshot);
        }

        [Fact]
        public void Publish_PublishesToSetPublishFilePath()
        {
            // Arrange
            var serializationSuccessful = false;
            var omniSharpProjectSnapshot = CreateProjectSnapshot("/path/to/project.csproj");
            var expectedPublishFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestProjectChangePublisher(
                LoggerFactory,
                onSerializeToFile: (snapshot, publishFilePath) =>
                {
                    Assert.Same(omniSharpProjectSnapshot, snapshot);
                    Assert.Equal(expectedPublishFilePath, publishFilePath);
                    serializationSuccessful = true;
                });
            publisher.SetPublishFilePath(omniSharpProjectSnapshot.FilePath, expectedPublishFilePath);

            // Act
            publisher.Publish(omniSharpProjectSnapshot);

            // Assert
            Assert.True(serializationSuccessful);
        }

        [Fact]
        public async Task ProjectRemoved_UnSetPublishFilePath_Noops()
        {
            // Arrange
            var snapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
            var publisher = new TestProjectChangePublisher(LoggerFactory);
            publisher.Initialize(snapshotManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default, "TestRootNamespace");
            await RunOnForegroundAsync(() => snapshotManager.ProjectAdded(hostProject));

            // Act & Assert
            await RunOnForegroundAsync(() => snapshotManager.ProjectRemoved(hostProject));
        }

        private class TestProjectChangePublisher : DefaultProjectChangePublisher
        {
            private readonly Action<OmniSharpProjectSnapshot, string> _onSerializeToFile;

            public TestProjectChangePublisher(
                ILoggerFactory loggerFactory,
                Action<OmniSharpProjectSnapshot, string> onSerializeToFile = null
            ) : base(loggerFactory)
            {
                _onSerializeToFile = onSerializeToFile ?? ((_, __) => throw new XunitException("SerializeToFile should not have been called."));
            }

            protected override void SerializeToFile(OmniSharpProjectSnapshot projectSnapshot, string publishFilePath) => _onSerializeToFile?.Invoke(projectSnapshot, publishFilePath);
        }
    }
}
