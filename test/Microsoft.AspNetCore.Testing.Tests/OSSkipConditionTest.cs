// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNetCore.Testing.xunit
{
    public class OSSkipConditionTest
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        public void TestSkipLinux()
        {
            Assert.False(
                PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Linux,
                "Test should not be running on Linux");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void TestSkipMacOSX()
        {
            Assert.False(
                PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Darwin,
                "Test should not be running on MacOSX.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public void RunTest_DoesNotRunOnWin7OrWin2008R2()
        {
            Assert.False(
                PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Windows &&
                PlatformServices.Default.Runtime.OperatingSystemVersion == "6.1",
                "Test should not be running on Win7 or Win2008R2.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        public void TestSkipWindows()
        {
            Assert.False(
                PlatformServices.Default.Runtime.OperatingSystemPlatform == Platform.Windows,
                "Test should not be running on Windows.");
        }
    }
}
