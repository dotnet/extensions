// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.Testing
{
    public static partial class DumpCollector
    {
        public static void Collect(Process process, string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Windows.Collect(process, fileName);
            }
        }

        public static Task CollectAsync(Process process, string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Windows.CollectAsync(process, fileName);
            }
            else
            {
                // No implementations yet for macOS and Linux
                return Task.CompletedTask;
            }
        }
    }
}
