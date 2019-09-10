// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Execution;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class MSBuildProjectDocumentChangeDetectorTest
    {
        [Fact]
        public void FileSystemWatcher_RazorDocumentEvent_InvokesOutputListeners()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            void AssertCallbackArgs(RazorFileChangeEventArgs args)
            {
                Assert.Equal("/path/to/file.cshtml", args.FilePath);
                Assert.Equal("file.cshtml", args.RelativeFilePath);
                Assert.Equal(RazorFileChangeKind.Removed, args.Kind);
                Assert.Same(projectInstance, args.UnevaluatedProjectInstance);
            }

            var listener1 = new Mock<IRazorDocumentChangeListener>();
            listener1.Setup(listener => listener.RazorDocumentChanged(It.IsAny<RazorFileChangeEventArgs>()))
                .Callback<RazorFileChangeEventArgs>((args) => AssertCallbackArgs(args))
                .Verifiable();
            var listener2 = new Mock<IRazorDocumentChangeListener>();
            listener2.Setup(listener => listener.RazorDocumentChanged(It.IsAny<RazorFileChangeEventArgs>()))
                .Callback<RazorFileChangeEventArgs>((args) => AssertCallbackArgs(args))
                .Verifiable();
            var detector = new MSBuildProjectDocumentChangeDetector(
                new[] { listener1.Object, listener2.Object },
                Enumerable.Empty<IRazorDocumentOutputChangeListener>());

            // Act
            detector.FileSystemWatcher_RazorDocumentEvent("/path/to/file.cshtml", "/path/to", projectInstance, RazorFileChangeKind.Removed);

            // Assert
            listener1.VerifyAll();
            listener2.VerifyAll();
        }

        [Fact]
        public void FileSystemWatcher_RazorDocumentOutputEvent_InvokesOutputListeners()
        {
            // Arrange
            var projectInstance = new ProjectInstance(ProjectRootElement.Create());
            void AssertCallbackArgs(RazorFileChangeEventArgs args)
            {
                Assert.Equal("/path/to/file.cshtml", args.FilePath);
                Assert.Equal("file.cshtml", args.RelativeFilePath);
                Assert.Equal(RazorFileChangeKind.Removed, args.Kind);
                Assert.Same(projectInstance, args.UnevaluatedProjectInstance);
            }

            var listener1 = new Mock<IRazorDocumentOutputChangeListener>();
            listener1.Setup(listener => listener.RazorDocumentOutputChanged(It.IsAny<RazorFileChangeEventArgs>()))
                .Callback<RazorFileChangeEventArgs>((args) => AssertCallbackArgs(args))
                .Verifiable();
            var listener2 = new Mock<IRazorDocumentOutputChangeListener>();
            listener2.Setup(listener => listener.RazorDocumentOutputChanged(It.IsAny<RazorFileChangeEventArgs>()))
                .Callback<RazorFileChangeEventArgs>((args) => AssertCallbackArgs(args))
                .Verifiable();
            var detector = new MSBuildProjectDocumentChangeDetector(
                Enumerable.Empty<IRazorDocumentChangeListener>(),
                new[] { listener1.Object, listener2.Object });

            // Act
            detector.FileSystemWatcher_RazorDocumentOutputEvent("/path/to/file.cshtml", "/path/to", projectInstance, RazorFileChangeKind.Removed);

            // Assert
            listener1.VerifyAll();
            listener2.VerifyAll();
        }

        [Fact]
        public void ResolveRelativeFilePath_NotRelative_ReturnsOriginalPath()
        {
            // Arrange
            var filePath = "file.cshtml";
            var projectDirectory = "/path/project";

            // Act
            var relativePath = MSBuildProjectDocumentChangeDetector.ResolveRelativeFilePath(filePath, projectDirectory);

            // Assert
            Assert.Equal(filePath, relativePath);
        }

        [Fact]
        public void ResolveRelativeFilePath_Relative_ReturnsRelativePath()
        {
            // Arrange
            var filePath = "/path/to/file.cshtml";
            var projectDirectory = "/path/to";

            // Act
            var relativePath = MSBuildProjectDocumentChangeDetector.ResolveRelativeFilePath(filePath, projectDirectory);

            // Assert
            Assert.Equal("file.cshtml", relativePath);
        }
    }
}
