// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Test.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    public class RazorCodeDocumentExtensionsTest
    {
        [Fact]
        public void IsUnsupported_Unset_ReturnsFalse()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();

            // Act
            var result = codeDocument.IsUnsupported();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsUnsupported_Set_ReturnsTrue()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            codeDocument.SetUnsupported();

            // Act
            var result = codeDocument.IsUnsupported();

            // Assert
            Assert.True(result);
        }
    }
}
