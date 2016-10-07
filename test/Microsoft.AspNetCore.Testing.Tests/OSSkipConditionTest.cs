// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Testing.xunit
{
    public class OSSkipConditionTest
    {
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux)]
        public void TestSkipLinux()
        {
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                "Test should not be running on Linux");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public void TestSkipMacOSX()
        {
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX),
                "Test should not be running on MacOSX.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win7, WindowsVersions.Win2008R2)]
        public void RunTest_DoesNotRunOnWin7OrWin2008R2()
        {
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                Microsoft.Extensions.Internal.RuntimeEnvironment.OperatingSystemVersion.StartsWith("6.1"),
                "Test should not be running on Win7 or Win2008R2.");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows)]
        public void TestSkipWindows()
        {
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "Test should not be running on Windows.");
        }
    }

    [OSSkipCondition(OperatingSystems.Windows)]
    public class OSSkipConditionClassTest
    {
        [ConditionalFact]
        public void TestSkipClassWindows()
        {
            Assert.False(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
                "Test should not be running on Windows.");
        }
    }
}
