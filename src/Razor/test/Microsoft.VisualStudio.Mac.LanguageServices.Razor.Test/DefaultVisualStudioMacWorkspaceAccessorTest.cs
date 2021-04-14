// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Editor.Razor;
using Microsoft.VisualStudio.Text;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Mac.LanguageServices.Razor
{
    public class DefaultVisualStudioMacWorkspaceAccessorTest
    {
        [Fact]
        public void TryGetWorkspace_NoHostProject_ReturnsFalse()
        {
            // Arrange
            var textBufferProjectService = new Mock<TextBufferProjectService>(MockBehavior.Strict);
            textBufferProjectService.Setup(s => s.GetHostProject(It.IsAny<ITextBuffer>())).Returns(value: null);
            var workspaceAccessor = new DefaultVisualStudioMacWorkspaceAccessor(textBufferProjectService.Object);
            var textBuffer = Mock.Of<ITextBuffer>(MockBehavior.Strict);

            // Act
            var result = workspaceAccessor.TryGetWorkspace(textBuffer, out var workspace);

            // Assert
            Assert.False(result);
        }

        // -------------------------------------------------------------------------------------------
        // Purposefully do not have any more tests here because that would involve mocking MonoDevelop 
        // types. The default constructors for the Solution / DotNetProject MonoDevelop types change
        // static classes (they assume they're being created in an IDE).
        // -------------------------------------------------------------------------------------------
    }
}
