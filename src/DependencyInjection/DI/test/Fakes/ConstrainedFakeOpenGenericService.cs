// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.Fakes
{
    public class ConstrainedFakeOpenGenericService<TVal> : IFakeOpenGenericService<TVal>
        where TVal : PocoClass
    {
        public ConstrainedFakeOpenGenericService(TVal value)
        {
            Value = value;
        }
        public TVal Value { get; }
    }
}
