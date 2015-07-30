// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;

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
        private static Lazy<bool> _isWindows = new Lazy<bool>(() => RuntimeEnvironment.OperatingSystem.Equals("Windows", StringComparison.OrdinalIgnoreCase));
        private static Lazy<bool> _isLinux = new Lazy<bool>(() => RuntimeEnvironment.OperatingSystem.Equals("Linux", StringComparison.OrdinalIgnoreCase));
        private static Lazy<bool> _isMac = new Lazy<bool>(() => RuntimeEnvironment.OperatingSystem.Equals("Darwin", StringComparison.OrdinalIgnoreCase));

        public static bool IsMono { get { return _isMono.Value; } }
        public static bool IsWindows { get { return _isWindows.Value; } }
        public static bool IsLinux { get { return _isLinux.Value; } }
        public static bool IsMac { get { return _isMac.Value; } }

        internal static IRuntimeEnvironment RuntimeEnvironment { get { return _runtimeEnv.Value; } }
    }
}