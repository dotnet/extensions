// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.Framework.TestHost.Client
{
    public static class DNX
    {
        public static string FindDnx()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "cmd",
                Arguments = "/c where dnx",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            process.Start();
            process.WaitForExit();
            return process.StandardOutput.ReadToEnd().TrimEnd('\r', '\n');
        }
    }
}
