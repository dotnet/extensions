// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System.Collections;
namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithInterfaceConstraint<T> : IFakeOpenGenericService<T>
        where T : IEnumerable
    {
        public T Value { get; set; }
    }
}
