// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithClassConstraint<T> : IFakeOpenGenericService<T>
        where T : class
    {
        public ClassWithClassConstraint(T value) => Value = value;

        public T Value { get; }
    }
}
