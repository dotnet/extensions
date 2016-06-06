// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Testing.xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class FrameworkSkipConditionAttribute : Attribute, ITestCondition
    {
        private readonly RuntimeFrameworks _excludedFrameworks;

        public FrameworkSkipConditionAttribute(RuntimeFrameworks excludedFrameworks)
        {
            _excludedFrameworks = excludedFrameworks;
        }

        public bool IsMet
        {
            get
            {
                return CanRunOnThisFramework(_excludedFrameworks);
            }
        }

        public string SkipReason { get; set; } = "Test cannot run on this runtime framework.";

        private static bool CanRunOnThisFramework(RuntimeFrameworks excludedFrameworks)
        {
            if (excludedFrameworks == RuntimeFrameworks.None)
            {
                return true;
            }

#if NET451
            if (excludedFrameworks.HasFlag(RuntimeFrameworks.Mono) &&
                TestPlatformHelper.IsMono)
            {
                return false;
            }

            if (excludedFrameworks.HasFlag(RuntimeFrameworks.CLR))
            {
                return false;
            }
#else
            if (excludedFrameworks.HasFlag(RuntimeFrameworks.CoreCLR))
            {
                return false;
            }
#endif
            return true;
        }
    }
}