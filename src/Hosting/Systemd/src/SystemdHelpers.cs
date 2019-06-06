// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Extensions.Hosting.Systemd
{
    /// <summary>
    /// Helper methods for systemd Services.
    /// </summary>
    public static class SystemdHelpers
    {
        private const string INVOCATION_ID = "INVOCATION_ID";

        /// <summary>
        /// Check if the current process is hosted as a systemd Service.
        /// </summary>
        /// <returns><c>True</c> if the current process is hosted as a systemd Service, otherwise <c>false</c>.</returns>
        public static bool IsSystemdService ()
        {
            // We use the INVOCATION_ID envirionment variable that was introduced in systemd 232 (released 2016-11-03).
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(INVOCATION_ID));
        }
    }
}
