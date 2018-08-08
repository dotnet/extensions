// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public abstract class TestBase : ForegroundDispatcherTestBase
    {
        public TestBase()
        {
            var dispatcherObject = typeof(ForegroundDispatcherTestBase)
                .GetProperty("Dispatcher", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .GetValue(this);

            Dispatcher = ForegroundDispatcherShim.AsDispatcher(dispatcherObject);
        }

        public ForegroundDispatcherShim Dispatcher { get; }
    }
}
