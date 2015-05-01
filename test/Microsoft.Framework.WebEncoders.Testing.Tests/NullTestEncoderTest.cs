// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Framework.WebEncoders.Testing
{
    public class NullTestEncoderTest
    {
        [Fact]
        public void Encode_returnsSameValue()
        {
            // Arrange
            var encoder = new NullTestEncoder();
            var input = "TestValue";

            // Act
            var htmlEncodedValue = encoder.HtmlEncode(input);
            var javaScriptStringEncodedValue = encoder.JavaScriptStringEncode(input);
            var urlEncodedValue = encoder.UrlEncode(input);

            // Assert
            Assert.Equal(input, htmlEncodedValue);
            Assert.Equal(input, javaScriptStringEncodedValue);
            Assert.Equal(input, urlEncodedValue);
        }
    }
}