// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class ActivatorUtilitiesTests
    {
        [Fact]
        public void CreateInstance_WithAbstractTypeAndPublicConstructor_ThrowsCorrectException()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => ActivatorUtilities.CreateInstance(default(IServiceProvider), typeof(AbstractFoo)));
            var msg = "A suitable constructor for type 'Microsoft.Extensions.Internal.AbstractFoo' could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor.";
            Assert.Equal(msg, ex.Message);
        }
    }

    abstract class AbstractFoo
    {
        // The constructor should be public, since that is checked as well.
        public AbstractFoo()
        {
        }
    }
}
