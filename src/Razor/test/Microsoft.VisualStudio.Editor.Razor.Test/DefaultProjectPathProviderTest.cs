// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultProjectPathProviderTest
    {
        [Fact]
        public void TryGetProjectPath_NullLiveShareProjectPathProvider_UsesProjectService()
        {
            // Arrange
            var expectedProjectPath = "/my/project/path.csproj";
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(service => service.GetHostProject(It.IsAny<ITextBuffer>()))
                .Returns(new object());
            projectService.Setup(service => service.GetProjectPath(It.IsAny<object>()))
                .Returns(expectedProjectPath);
            var projectPathProvider = new DefaultProjectPathProvider(projectService.Object, liveShareProjectPathProvider: null);
            var textBuffer = Mock.Of<ITextBuffer>();

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedProjectPath, filePath);
        }

        [Fact]
        public void TryGetProjectPath_PrioritizesLiveShareProjectPathProvider()
        {
            // Arrange
            var liveShareProjectPathProvider = new Mock<LiveShareProjectPathProvider>();
            var liveShareProjectPath = "/path/from/liveshare.csproj";
            liveShareProjectPathProvider.Setup(provider => provider.TryGetProjectPath(It.IsAny<ITextBuffer>(), out liveShareProjectPath))
                .Returns(true);
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(service => service.GetHostProject(It.IsAny<ITextBuffer>()))
                .Throws<XunitException>();
            var projectPathProvider = new DefaultProjectPathProvider(projectService.Object, liveShareProjectPathProvider.Object);
            var textBuffer = Mock.Of<ITextBuffer>();

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.True(result);
            Assert.Equal(liveShareProjectPath, filePath);
        }

        [Fact]
        public void TryGetProjectPath_ReturnsFalseIfNoProject()
        {
            // Arrange
            var projectPathProvider = new DefaultProjectPathProvider(Mock.Of<TextBufferProjectService>(), Mock.Of<LiveShareProjectPathProvider>());
            var textBuffer = Mock.Of<ITextBuffer>();

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.False(result);
            Assert.Null(filePath);
        }

        [Fact]
        public void TryGetProjectPath_ReturnsTrueIfProject()
        {
            // Arrange
            var expectedProjectPath = "/my/project/path.csproj";
            var projectService = new Mock<TextBufferProjectService>();
            projectService.Setup(service => service.GetHostProject(It.IsAny<ITextBuffer>()))
                .Returns(new object());
            projectService.Setup(service => service.GetProjectPath(It.IsAny<object>()))
                .Returns(expectedProjectPath);
            var projectPathProvider = new DefaultProjectPathProvider(projectService.Object, Mock.Of<LiveShareProjectPathProvider>());
            var textBuffer = Mock.Of<ITextBuffer>();

            // Act
            var result = projectPathProvider.TryGetProjectPath(textBuffer, out var filePath);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedProjectPath, filePath);
        }
    }
}
