// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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