// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithAmbiguousCtors
    {
        public ClassWithAmbiguousCtors(string data)
        {
        }

        public ClassWithAmbiguousCtors(IFakeService service, string data)
        {
        }

        public ClassWithAmbiguousCtors(IFakeService service, int data)
        {
        }

        public ClassWithAmbiguousCtors(IFakeService service, string data1, int data2)
        {
            FakeService = service;
            Data1 = data1;
            Data2 = data2;
        }

        public IFakeService FakeService { get; }

        public string Data1 { get; }

        public int Data2 { get; }
    }
}