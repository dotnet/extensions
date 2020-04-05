// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    public class FilePathNormalizerTest
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Test only valid on Windows boxes")]
        public void Normalize_Windows_StripsPrecedingSlash()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var path = "/c:/path/to/something";

            // Act
            path = filePathNormalizer.Normalize(path);

            // Assert
            Assert.Equal("c:/path/to/something", path);
        }

        [Fact]
        public void Normalize_IgnoresUNCPaths()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var path = "//ComputerName/path/to/something";

            // Act
            path = filePathNormalizer.Normalize(path);

            // Assert
            Assert.Equal("//ComputerName/path/to/something", path);
        }

        [Fact]
        public void NormalizeDirectory_EndsWithSlash()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var directory = "C:\\path\\to\\directory\\";

            // Act
            var normalized = filePathNormalizer.NormalizeDirectory(directory);

            // Assert
            Assert.Equal("C:/path/to/directory/", normalized);
        }

        [Fact]
        public void NormalizeDirectory_EndsWithoutSlash()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var directory = "C:\\path\\to\\directory";

            // Act
            var normalized = filePathNormalizer.NormalizeDirectory(directory);

            // Assert
            Assert.Equal("C:/path/to/directory/", normalized);
        }

        [Fact]
        public void FilePathsEquivalent_NotEqualPaths_ReturnsFalse()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath1 = "path/to/document.cshtml";
            var filePath2 = "path\\to\\different\\document.cshtml";

            // Act
            var result = filePathNormalizer.FilePathsEquivalent(filePath1, filePath2);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void FilePathsEquivalent_NormalizesPathsBeforeComparison_ReturnsTrue()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath1 = "path/to/document.cshtml";
            var filePath2 = "path\\to\\document.cshtml";

            // Act
            var result = filePathNormalizer.FilePathsEquivalent(filePath1, filePath2);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetDirectory_IncludesTrailingSlash()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath = "C:/path/to/document.cshtml";

            // Act
            var normalized = filePathNormalizer.GetDirectory(filePath);

            // Assert
            Assert.Equal("C:/path/to/", normalized);
        }

        [Fact]
        public void GetDirectory_NoDirectory_ReturnsRoot()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath = "C:/document.cshtml";

            // Act
            var normalized = filePathNormalizer.GetDirectory(filePath);

            // Assert
            Assert.Equal("C:/", normalized);
        }

        [Fact]
        public void Normalize_NullFilePath_ReturnsForwardSlash()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();

            // Act
            var normalized = filePathNormalizer.Normalize(null);

            // Assert
            Assert.Equal("/", normalized);
        }

        [Fact]
        public void Normalize_EmptyFilePath_ReturnsEmptyString()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();

            // Act
            var normalized = filePathNormalizer.Normalize(null);

            // Assert
            Assert.Equal("/", normalized);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Test only valid on non-Windows boxes")]
        public void Normalize_NonWindows_AddsLeadingForwardSlash()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath = "path/to/document.cshtml";

            // Act
            var normalized = filePathNormalizer.Normalize(filePath);

            // Assert
            Assert.Equal("/path/to/document.cshtml", normalized);
        }

        [Fact]
        public void Normalize_UrlDecodesFilePath()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath = "C:/path%20to/document.cshtml";

            // Act
            var normalized = filePathNormalizer.Normalize(filePath);

            // Assert
            Assert.Equal("C:/path to/document.cshtml", normalized);
        }

        [Fact]
        public void Normalize_ReplacesBackSlashesWithForwardSlashes()
        {
            // Arrange
            var filePathNormalizer = new FilePathNormalizer();
            var filePath = "C:\\path\\to\\document.cshtml";

            // Act
            var normalized = filePathNormalizer.Normalize(filePath);

            // Assert
            Assert.Equal("C:/path/to/document.cshtml", normalized);
        }
    }
}
