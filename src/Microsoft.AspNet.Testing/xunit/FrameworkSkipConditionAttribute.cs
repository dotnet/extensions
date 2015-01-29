// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Testing.xunit
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

        public string SkipReason
        {
            get
            {
                return "Test cannot run on this runtime framework.";
            }
        }

        private static bool CanRunOnThisFramework(RuntimeFrameworks excludedFrameworks)
        {
            if (excludedFrameworks == RuntimeFrameworks.None)
            {
                return true;
            }

            if (excludedFrameworks.HasFlag(RuntimeFrameworks.Mono) &&
                  TestPlatformHelper.IsMono)
            {
                return false;
            }

            if (excludedFrameworks.HasFlag(RuntimeFrameworks.DotNet) &&
                 !TestPlatformHelper.IsMono)
            {
                return false;
            }

            return true;
        }
    }
}