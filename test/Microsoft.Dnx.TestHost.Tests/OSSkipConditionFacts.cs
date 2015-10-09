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
        private IRuntimeEnvironment RuntimeEnvironment
        {
            get
            {
                return (IRuntimeEnvironment)CallContextServiceLocator
                        .Locator
                        .ServiceProvider
                        .GetService(typeof(IRuntimeEnvironment));
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        public void TestSkipLinux()
        {
            Assert.False(
                "linux" == RuntimeEnvironment.OperatingSystem.ToLowerInvariant(),
                "Test should not be running on Linux");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void TestSkipMacOSX()
        {
            Assert.False(
                "darwin" == RuntimeEnvironment.OperatingSystem.ToLowerInvariant(),
                "Test should not be running on MacOSX.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public void RunTest_DoesNotRunOnWin7OrWin2008R2()
        {
            var osVersion = Environment.OSVersion.Version;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT
                && osVersion.Major == 6 && osVersion.Minor == 1)
            {
                Assert.False(true, "Test should not be running on Win7 or Win2008R2.");
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        public void TestSkipWindows()
        {
            Assert.False(
                "windows" == RuntimeEnvironment.OperatingSystem.ToLowerInvariant(),
                "Test should not be running on Windows.");
        }
    }
}
