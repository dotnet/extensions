// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithSelfReferencingConstraint<T> : IFakeOpenGenericService<T>
        where T : IComparable<T>
    {
        public T Value { get; } = default;
    }
}
