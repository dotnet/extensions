// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Testing.xunit;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Hosting
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
    public class HasSystemdUserServiceCondition : Attribute, ITestCondition
    {
        public HasSystemdUserServiceCondition()
        {}

        public bool IsMet
        {
            get
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return false;
                }

                string userProcesses = ProcessHelper.RunProcess("ps", $"-u {Environment.UserName} -o command");
                return userProcesses.Contains("systemd --user");
            }
        }

        public string SkipReason { get; set; } = "Test cannot run on without systemd user instance.";
    }
}