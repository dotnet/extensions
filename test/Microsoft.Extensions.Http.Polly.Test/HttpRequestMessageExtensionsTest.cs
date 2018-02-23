// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Xunit;

namespace Polly
{ 
    public class HttpRequestMessageExtensionsTest
    {
        [Fact]
        public void GetPollyContext_Found_SetsContext()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var expected = new Context(Guid.NewGuid().ToString());
            request.Properties[HttpRequestMessageExtensions.PollyContextKey] = expected;

            // Act
            var actual = request.GetPollyContext();

            // Assert
            Assert.Same(expected, actual);
        }

        [Fact]
        public void GetPollyContext_NotFound_ReturnsNull()
        {
            // Arrange
            var request = new HttpRequestMessage();

            // Act
            var actual = request.GetPollyContext();

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void GetPollyContext_Null_ReturnsNull()
        {
            // Arrange
            var request = new HttpRequestMessage();
            request.Properties[HttpRequestMessageExtensions.PollyContextKey] = null;

            // Act
            var actual = request.GetPollyContext();

            // Assert
            Assert.Null(actual);
        }

        [Fact]
        public void SetPollyContext_WithValue_SetsContext()
        {
            // Arrange
            var request = new HttpRequestMessage();
            var expected = new Context(Guid.NewGuid().ToString());

            // Act
            request.SetPollyContext(expected);

            // Assert
            var actual = request.Properties[HttpRequestMessageExtensions.PollyContextKey];
            Assert.Same(expected, actual);
        }

        [Fact]
        public void SetPollyContext_WithNull_SetsNull()
        {
            // Arrange
            var request = new HttpRequestMessage();
            request.Properties[HttpRequestMessageExtensions.PollyContextKey] = new Context(Guid.NewGuid().ToString());

            // Act
            request.SetPollyContext(null);

            // Assert
            var actual = request.Properties[HttpRequestMessageExtensions.PollyContextKey];
            Assert.Null(actual);
        }
    }
}
