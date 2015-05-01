// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Framework.WebEncoders.Testing
{
    public class CommonTestEncoderTest
    {
        [Fact]
        public void Encode_returnsEncodedValue()
        {
            // Arrange
            var encoder = new CommonTestEncoder();
            var input = "TestValue";

            // Act
            var htmlEncodedValue = encoder.HtmlEncode(input);
            var javaScriptStringEncodedValue = encoder.JavaScriptStringEncode(input);
            var urlEncodedValue = encoder.UrlEncode(input);

            // Assert
            Assert.Equal($"HtmlEncode[[{input}]]", htmlEncodedValue);
            Assert.Equal($"JavaScriptStringEncode[[{input}]]", javaScriptStringEncodedValue);
            Assert.Equal($"UrlEncode[[{input}]]", urlEncodedValue);
        }
    }
}