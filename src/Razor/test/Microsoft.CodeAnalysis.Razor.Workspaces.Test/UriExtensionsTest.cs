// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class UriExtensionsTest
    {
        [Fact]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Test only valid on Windows boxes")]
        public void GetAbsoluteOrUNCPath_ReturnsAbsolutePath()
        {
            // Arrange
            var uri = new Uri("c:\\Some\\path\\to\\file.cshtml");

            // Act
            var path = uri.GetAbsoluteOrUNCPath();

            // Assert
            Assert.Equal(uri.AbsolutePath, path);
        }

        [Fact]
        public void GetAbsoluteOrUNCPath_UNCPath_ReturnsLocalPath()
        {
            // Arrange
            var uri = new Uri("//Some/path/to/file.cshtml");

            // Act
            var path = uri.GetAbsoluteOrUNCPath();

            // Assert
            Assert.Equal(uri.LocalPath, path);
        }
    }
}
