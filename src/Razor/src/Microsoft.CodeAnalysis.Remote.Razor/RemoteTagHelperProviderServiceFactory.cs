// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote.Razor
{
    internal sealed class RemoteTagHelperProviderServiceFactory : RazorServiceFactoryBase<IRemoteTagHelperProviderService>
    {
        public RemoteTagHelperProviderServiceFactory() : base(RazorServiceDescriptors.TagHelperProviderServiceDescriptors)
        {
        }

        protected override IRemoteTagHelperProviderService CreateService(IServiceBroker serviceBroker)
                => new RemoteTagHelperProviderService(serviceBroker);
    }
}
