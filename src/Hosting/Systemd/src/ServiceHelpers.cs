// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Extensions.Hosting.Systemd
{
    /// <summary>
    /// Helper methods for systemd Services.
    /// </summary>
    public static class ServiceHelpers
    {
        const string INVOCATION_ID = "INVOCATION_ID";

        private static string s_invocationId;

        /// <summary>
        /// Check if the current process is hosted as a systemd Service.
        /// </summary>
        /// <returns><c>True</c> if the current process is hosted as a systemd Service, otherwise <c>false</c>.</returns>
        public static bool IsSystemd()
        {
            // We use the INVOCATION_ID envirionment variable that was introduced in systemd 232 (released 2016-11-03).
            // We clear the envvar so .NET child processes return false for this method.
            if (s_invocationId == null)
            {
                string invocationId = Environment.GetEnvironmentVariable(INVOCATION_ID);
                if (invocationId == null)
                {
                    invocationId = "";
                }
                if (Interlocked.CompareExchange(ref s_invocationId, invocationId, null) == null)
                {
                    Environment.SetEnvironmentVariable(INVOCATION_ID, null);
                }
            }
            return s_invocationId.Length != 0;
        }
    }
}