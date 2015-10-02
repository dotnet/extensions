// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Testing.xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OSSkipConditionAttribute : Attribute, ITestCondition
    {
        private readonly OperatingSystems _excludedOS;

        public OSSkipConditionAttribute(OperatingSystems excludedOperatingSystems)
        {
            _excludedOS = excludedOperatingSystems;
        }

        public bool IsMet
        {
            get
            {
                return CanRunOnThisOS(_excludedOS);
            }
        }

        public string SkipReason { get; set; } = "Test cannot run on this operating system.";

        private static bool CanRunOnThisOS(OperatingSystems excludedOperatingSystems)
        {
            if (excludedOperatingSystems == OperatingSystems.None)
            {
                return true;
            }

            switch (TestPlatformHelper.RuntimeEnvironment.OperatingSystem.ToLowerInvariant())
            {
                case "windows":
                    var osVersion = TestPlatformHelper.RuntimeEnvironment.OperatingSystemVersion;

                    if (osVersion.Equals("7.0", StringComparison.OrdinalIgnoreCase) &&
                        (excludedOperatingSystems.HasFlag(OperatingSystems.Win7) || excludedOperatingSystems.HasFlag(OperatingSystems.Win2008R2)))
                    {
                        return false;
                    }
                    break;
                case "linux":
                    if (excludedOperatingSystems.HasFlag(OperatingSystems.Linux))
                    {
                        return false;
                    }
                    break;
                case "darwin":
                    if (excludedOperatingSystems.HasFlag(OperatingSystems.MacOSX))
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }
    }
}
