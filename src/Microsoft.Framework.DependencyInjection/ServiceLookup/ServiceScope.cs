// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.DependencyInjection.ServiceLookup
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
            get { return _scopedProvider.GetService<IServiceProvider>(); }
        }

        public void Dispose()
        {
            _scopedProvider.Dispose();
        }
    }
}
