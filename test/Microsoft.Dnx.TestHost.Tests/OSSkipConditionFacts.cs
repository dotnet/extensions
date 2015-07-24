// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Infrastructure;
using Xunit;

namespace Microsoft.Dnx.TestHost.Tests
{
    public class OSSkipConditionFacts
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        public void TestSkipLinux()
        {
            var env = (IRuntimeEnvironment)CallContextServiceLocator
                .Locator
                .ServiceProvider
                .GetService(typeof(IRuntimeEnvironment));

            Assert.False("Linux" == env.OperatingSystem, "Test should not be running on Linux");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void TestSkipMacOSX()
        {
            var env = (IRuntimeEnvironment)CallContextServiceLocator
                .Locator
                .ServiceProvider
                .GetService(typeof(IRuntimeEnvironment));

            Assert.False("Darwin" == env.OperatingSystem, "Test should not be running on MacOSX");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Win7And2008R2)]
        public void RunTest_DoesNotRunOnWin7()
        {
            Version osVersion = Environment.OSVersion.Version;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT
                && osVersion.Major == 6 && osVersion.Minor == 1)
            {
                Assert.False(true, "Test should not be running on Win7");
            }
        }
    }
}
