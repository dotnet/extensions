// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    public class TypeWithNewConstraint<T> : IFakeOpenGenericService<T>
        where T : new()
    {
        public T Value { get; } = default;
    }
}
