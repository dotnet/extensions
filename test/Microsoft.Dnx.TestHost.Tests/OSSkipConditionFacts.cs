// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.Dnx.TestHost.Tests
{
    public class OSSkipConditionFacts
    {
        private IRuntimeEnvironment RuntimeEnvironment
        {
            get
            {
                return PlatformServices.Default.Runtime;
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
            Assert.False(
                RuntimeEnvironment.OperatingSystem.ToLowerInvariant() == "windows" &&
                RuntimeEnvironment.OperatingSystemVersion == "6.1",
                "Test should not be running on Win7 or Win2008R2.");
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
