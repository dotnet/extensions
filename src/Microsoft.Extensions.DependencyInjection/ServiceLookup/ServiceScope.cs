// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ServiceScope : IServiceScope
    {
        private readonly ServiceProvider _scopedProvider;

        public ServiceScope(ServiceProvider scopedProvider)
        {
            _scopedProvider = scopedProvider;
        }

        public IServiceProvider ServiceProvider
        {
            get { return _scopedProvider; }
        }

        public void Dispose()
        {
            _scopedProvider.Dispose();
        }
    }
}
