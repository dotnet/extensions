// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;

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

        public static string FindDnxDirectory()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".dnx\\runtimes");

            if (Directory.Exists(path))
            {
                return path;
            }
            else
            {
                return null;
            }
        }
    }
}
