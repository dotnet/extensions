// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

namespace Microsoft.Framework.DependencyInjection.Tests.Fakes
{
    public class FakeOptionsSetupA : IOptionsSetup<FakeOptions>
    {
        public int Order {
            get { return -1; }
        }

        public void Setup(FakeOptions options)
        {
            options.Message += "A";
        }
    }

    public class FakeOptionsSetupB : IOptionsSetup<FakeOptions>
    {
        public int Order
        {
            get { return 10; }
        }

        public void Setup(FakeOptions options)
        {
            options.Message += "B";
        }
    }

    public class FakeOptionsSetupC : IOptionsSetup<FakeOptions>
    {
        public int Order
        {
            get { return 1000; }
        }

        public void Setup(FakeOptions options)
        {
            options.Message += "C";
        }
    }
}
