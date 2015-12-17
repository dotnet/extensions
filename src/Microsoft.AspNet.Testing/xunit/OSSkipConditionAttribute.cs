// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Testing.xunit
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class OSSkipConditionAttribute : Attribute, ITestCondition
    {
        private readonly OperatingSystems _excludedOperatingSystem;
        private readonly IEnumerable<string> _excludedVersions;
        private readonly IRuntimeEnvironment _runtimeEnvironment;

        public OSSkipConditionAttribute(OperatingSystems operatingSystem, params string[] versions) :
            this(operatingSystem, TestPlatformHelper.RuntimeEnvironment, versions)
        {
        }

        // to enable unit testing
        internal OSSkipConditionAttribute(
            OperatingSystems operatingSystem, IRuntimeEnvironment runtimeEnvironment, params string[] versions)
        {
            _excludedOperatingSystem = operatingSystem;
            _excludedVersions = versions ?? Enumerable.Empty<string>();
            _runtimeEnvironment = runtimeEnvironment;
        }

        public bool IsMet
        {
            get
            {
                var currentOSInfo = GetCurrentOSInfo();

                var skip = (_excludedOperatingSystem == currentOSInfo.OperatingSystem);
                if (_excludedVersions.Any())
                {
                    skip = skip
                        && _excludedVersions.Contains(currentOSInfo.Version, StringComparer.OrdinalIgnoreCase);
                }

                // Since a test would be excuted only if 'IsMet' is true, return false if we want to skip
                return !skip;
            }
        }

        public string SkipReason { get; set; } = "Test cannot run on this operating system.";

        private OSInfo GetCurrentOSInfo()
        {
            var currentOS = _runtimeEnvironment.OperatingSystem;
            var currentOSVersion = _runtimeEnvironment.OperatingSystemVersion;

            OperatingSystems os;
            switch (_runtimeEnvironment.OperatingSystemPlatform)
            {
                case Platform.Windows:
                    os = OperatingSystems.Windows;
                    break;
                case Platform.Linux:
                    os = OperatingSystems.Linux;
                    break;
                case Platform.Darwin:
                    os = OperatingSystems.MacOSX;
                    break;
                default:
                    throw new InvalidOperationException($"Unrecognized operating system '{_runtimeEnvironment.OperatingSystemPlatform}'.");
            }

            return new OSInfo()
            {
                OperatingSystem = os,
                Version = currentOSVersion
            };
        }

        private class OSInfo
        {
            public OperatingSystems OperatingSystem { get; set; }

            public string Version { get; set; }
        }
    }
}
