// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Extensions.Http
{
    public class DefaultHttpClientFactoryTest
    {
        [Fact]
        public void Factory_CanCreateClient()
        {
            // Arrange
            var factory = new DefaultHttpClientFactory();

            // Act
            var client = factory.CreateClient();

            // Assert
            Assert.NotNull(client);
        }
    }
}
