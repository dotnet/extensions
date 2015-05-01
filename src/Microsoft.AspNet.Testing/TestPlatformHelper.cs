// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Infrastructure;

namespace Microsoft.AspNet.Testing
{
    public static class TestPlatformHelper
    {
        private static Lazy<IRuntimeEnvironment> _runtimeEnv = new Lazy<IRuntimeEnvironment>(() =>
            (IRuntimeEnvironment)CallContextServiceLocator
            .Locator
            .ServiceProvider
            .GetService(typeof(IRuntimeEnvironment)));

        private static Lazy<bool> _isMono = new Lazy<bool>(() => RuntimeEnvironment.RuntimeType.Equals("Mono", StringComparison.OrdinalIgnoreCase));

        public static bool IsMono { get { return _isMono.Value; } }

        internal static IRuntimeEnvironment RuntimeEnvironment { get { return _runtimeEnv.Value; } }
    }
}