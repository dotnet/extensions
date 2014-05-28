// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.OptionsModel.Tests
{
    public class FakeOptions
    {
        public FakeOptions()
        {
            Message = "";
        }

        public string Message { get; set; }
    }
}
