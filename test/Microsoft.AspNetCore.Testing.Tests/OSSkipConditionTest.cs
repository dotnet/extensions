// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNetCore.Testing.xunit
{
    public class OSSkipConditionTest
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
                RuntimeEnvironment.OperatingSystemPlatform == Platform.Linux,
                "Test should not be running on Linux");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void TestSkipMacOSX()
        {
            Assert.False(
                RuntimeEnvironment.OperatingSystemPlatform == Platform.Darwin,
                "Test should not be running on MacOSX.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public void RunTest_DoesNotRunOnWin7OrWin2008R2()
        {
            Assert.False(
                RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows &&
                RuntimeEnvironment.OperatingSystemVersion == "6.1",
                "Test should not be running on Win7 or Win2008R2.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        public void TestSkipWindows()
        {
            Assert.False(
                RuntimeEnvironment.OperatingSystemPlatform == Platform.Windows,
                "Test should not be running on Windows.");
        }
    }
}
