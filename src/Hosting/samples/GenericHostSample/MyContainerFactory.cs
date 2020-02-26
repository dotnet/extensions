// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace GenericHostSample
{
    internal class MyContainerFactory : IServiceProviderFactory<MyContainer>
    {
        public MyContainer CreateBuilder(IServiceCollection services)
        {
            return new MyContainer();
        }

        public IServiceProvider CreateServiceProvider(MyContainer containerBuilder)
        {
            throw new NotImplementedException();
        }
    }
}