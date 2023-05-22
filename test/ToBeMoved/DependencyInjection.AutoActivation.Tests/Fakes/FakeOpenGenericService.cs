// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Test.Helpers;

namespace Microsoft.Extensions.DependencyInjection.Test.Fakes;

public class FakeOpenGenericService<TVal> : IFakeOpenGenericService<TVal>
{
    public FakeOpenGenericService(IFakeOpenGenericCounter count)
    {
        count.Counter += 1;
    }
}
