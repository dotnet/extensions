// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public abstract class TestBase : ForegroundDispatcherTestBase
    {
        public TestBase()
        {
            Logger = Mock.Of<VSCodeLogger>();
        }

        public VSCodeLogger Logger { get; }
    }
}
