// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Tooltip
{
    public class TypeNameStringResolverTest
    {
        [Fact]
        public void TryGetSimpleName_NonPrimitiveType_ReturnsFalse()
        {
            // Arrange
            var typeName = "Microsoft.AspNetCore.SomeType";

            // Act
            var result = TypeNameStringResolver.TryGetSimpleName(typeName, out var resolvedTypeName);

            // Assert
            Assert.False(result);
            Assert.Null(resolvedTypeName);
        }

        [Theory]
        [InlineData("System.Int32", "int")]
        [InlineData("System.Boolean", "bool")]
        [InlineData("System.String", "string")]
        public void GetSimpleName_SimplifiesPrimitiveTypes_ReturnsTrue(string typeName, string expectedTypeName)
        {
            // Arrange

            // Act
            var result = TypeNameStringResolver.TryGetSimpleName(typeName, out var resolvedTypeName);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedTypeName, resolvedTypeName);
        }
    }
}
