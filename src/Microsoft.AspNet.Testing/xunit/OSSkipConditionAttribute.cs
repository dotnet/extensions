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

        public string SkipReason
        {
            get
            {
                return "Test cannot run on this operating system.";
            }
        }

        private bool CanRunOnThisOS(OperatingSystems excludedOperatingSystems)
        {
            if (excludedOperatingSystems == OperatingSystems.None)
            {
                return true;
            }

            switch (TestPlatformHelper.RuntimeEnvironment.OperatingSystem)
            {
                case "Windows":
                    var osVersion = new Version(TestPlatformHelper.RuntimeEnvironment.OperatingSystemVersion);

                    // The GetVersion API has a back compat feature: for apps that are not manifested 
                    // and run on Windows 8.1, it returns version 6.2 rather than 6.3. See this:
                    // http://msdn.microsoft.com/en-us/library/windows/desktop/ms724439(v=vs.85).aspx
                    if (osVersion.Major == 6)
                    {
                        if (osVersion.Minor == 1 &&
                            (excludedOperatingSystems.HasFlag(OperatingSystems.Win7) ||
                            excludedOperatingSystems.HasFlag(OperatingSystems.Win2008R2)))
                        {
                            return false;
                        }
                    }
                    break;
                case "Linux":
                    if (excludedOperatingSystems.HasFlag(OperatingSystems.Linux))
                    {
                        return false;
                    }
                    break;
                case "Darwin":
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
