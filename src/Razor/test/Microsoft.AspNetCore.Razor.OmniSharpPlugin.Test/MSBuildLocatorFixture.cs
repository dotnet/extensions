// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Build.Locator;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class MSBuildLocatorFixture : IDisposable
    {
        public MSBuildLocatorFixture()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                MSBuildLocator.RegisterDefaults();
            }
        }

        public void Dispose()
        {
            MSBuildLocator.Unregister();
        }
    }
}
