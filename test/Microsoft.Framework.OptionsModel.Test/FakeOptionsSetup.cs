// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.OptionsModel.Tests
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
