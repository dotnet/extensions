// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Testing.xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class OSSkipConditionAttribute : Attribute, ITestCondition
    {
        private OperatingSystems _excludedOS;

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

        private static bool CanRunOnThisOS(OperatingSystems excludedOperatingSystems)
        {
            if (excludedOperatingSystems == OperatingSystems.None)
            {
                return true;
            }

            bool isWindows = false;
#if ASPNETCORE50
            Version osVersion = WindowsApis.OSVersion;

            // No platform check because it is always Windows
            isWindows = true;
#else
            Version osVersion = Environment.OSVersion.Version;

            switch (Environment.OSVersion.Platform)
            {
            case PlatformID.Win32NT:
                isWindows = true;
                break;
            case PlatformID.Unix:
                if (excludedOperatingSystems.HasFlag(OperatingSystems.Unix))
                {
                    return false;
                }
                break;
            }
#endif

            if (isWindows)
            {
                if (osVersion.Major == 6)
                {
                    if (osVersion.Minor == 1 &&
                        (excludedOperatingSystems.HasFlag(OperatingSystems.Win7) ||
                        excludedOperatingSystems.HasFlag(OperatingSystems.Win2008R2)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}