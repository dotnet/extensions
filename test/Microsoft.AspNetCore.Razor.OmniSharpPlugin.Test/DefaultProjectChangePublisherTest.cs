// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class DefaultProjectChangePublisherTest : OmniSharpTestBase
    {
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
            var attemptedToSerialize = false;
            var omniSharpProjectSnapshot = CreateProjectSnapshot("/path/to/project.csproj");
            var expectedPublishFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestProjectChangePublisher(
                LoggerFactory,
                onSerializeToFile: (snapshot, publishFilePath) =>
                {
                    attemptedToSerialize = true;

                    Assert.Same(omniSharpProjectSnapshot, snapshot);
                    Assert.Equal(expectedPublishFilePath, publishFilePath);
                });
            publisher.SetPublishFilePath(omniSharpProjectSnapshot.FilePath, expectedPublishFilePath);

            // Act
            publisher.Publish(omniSharpProjectSnapshot);

            // Assert
            Assert.True(attemptedToSerialize);
        }

        [Fact]
        public void ProjectAdded_PublishesToCorrectFilePath()
        {
            // Arrange
            var snapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
            var attemptedToSerialize = false;
            var expectedPublishFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestProjectChangePublisher(
                LoggerFactory,
                onSerializeToFile: (snapshot, publishFilePath) =>
                {
                    attemptedToSerialize = true;

                    Assert.Equal(expectedPublishFilePath, publishFilePath);
                });
            publisher.Initialize(snapshotManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default);
            publisher.SetPublishFilePath(hostProject.FilePath, expectedPublishFilePath);

            // Act
            snapshotManager.ProjectAdded(hostProject);

            // Assert
            Assert.True(attemptedToSerialize);
        }

        [Fact]
        public void ProjectChanged_PublishesToCorrectFilePath()
        {
            // Arrange
            var snapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
            var attemptsToSerialize = 0;
            var expectedPublishFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestProjectChangePublisher(
                LoggerFactory,
                onSerializeToFile: (snapshot, publishFilePath) =>
                {
                    attemptsToSerialize++;

                    Assert.Equal(expectedPublishFilePath, publishFilePath);
                });
            publisher.Initialize(snapshotManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default);
            publisher.SetPublishFilePath(hostProject.FilePath, expectedPublishFilePath);
            snapshotManager.ProjectAdded(hostProject);
            var newConfiguration = RazorConfiguration.Create(RazorLanguageVersion.Experimental, "Custom", Enumerable.Empty<RazorExtension>());
            var newHostProject = new OmniSharpHostProject(hostProject.FilePath, newConfiguration);

            // Act
            snapshotManager.ProjectConfigurationChanged(newHostProject);

            // Assert
            Assert.Equal(2, attemptsToSerialize);
        }

        [Fact]
        public void ProjectRemoved_UnSetPublishFilePath_Noops()
        {
            // Arrange
            var snapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
            var publisher = new TestProjectChangePublisher(LoggerFactory);
            publisher.Initialize(snapshotManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default);
            snapshotManager.ProjectAdded(hostProject);

            // Act & Assert
            snapshotManager.ProjectRemoved(hostProject);
        }

        [Fact]
        public void ProjectRemoved_DeletesPublishFile()
        {
            // Arrange
            var attemptedToDelete = false;
            var snapshotManager = CreateProjectSnapshotManager(allowNotifyListeners: true);
            var expectedPublishFilePath = "/path/to/obj/bin/Debug/project.razor.json";
            var publisher = new TestProjectChangePublisher(LoggerFactory,
                onSerializeToFile: (_, __) => { },
                onDeleteFile: (publishFilePath) =>
                {
                    attemptedToDelete = true;
                    Assert.Equal(expectedPublishFilePath, publishFilePath);
                });
            publisher.Initialize(snapshotManager);
            var hostProject = new OmniSharpHostProject("/path/to/project.csproj", RazorConfiguration.Default);
            publisher.SetPublishFilePath(hostProject.FilePath, expectedPublishFilePath);
            snapshotManager.ProjectAdded(hostProject);

            // Act
            snapshotManager.ProjectRemoved(hostProject);

            // Assert
            Assert.True(attemptedToDelete);
        }

        private class TestProjectChangePublisher : DefaultProjectChangePublisher
        {
            private readonly Action<OmniSharpProjectSnapshot, string> _onSerializeToFile;
            private readonly Action<string> _onDeleteFile;

            public TestProjectChangePublisher(
                ILoggerFactory loggerFactory,
                Action<OmniSharpProjectSnapshot, string> onSerializeToFile = null,
                Action<string> onDeleteFile = null) : base(loggerFactory)
            {
                _onSerializeToFile = onSerializeToFile ?? ((_, __) => throw new XunitException("SerializeToFile should not have been called."));
                _onDeleteFile = onDeleteFile ?? ((_) => throw new XunitException("DeleteFile should not have been called."));
            }

            protected override void SerializeToFile(OmniSharpProjectSnapshot projectSnapshot, string publishFilePath) => _onSerializeToFile?.Invoke(projectSnapshot, publishFilePath);

            protected override void DeleteFile(string publishFilePath) => _onDeleteFile?.Invoke(publishFilePath);
        }
    }
}
