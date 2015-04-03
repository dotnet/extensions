using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Framework.TestHost.UI
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
