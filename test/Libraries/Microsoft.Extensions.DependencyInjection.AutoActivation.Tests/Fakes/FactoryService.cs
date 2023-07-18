// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Test.Helpers;

namespace Microsoft.Extensions.DependencyInjection.Test.Fakes;

public class FactoryService : IFactoryService
{
    public FactoryService(IFakeService fakeService, IFactoryServiceCounter count)
    {
        FakeService = fakeService;
        count.Counter += 1;
    }

    public IFakeService FakeService { get; }
}
