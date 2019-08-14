using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Testing.xunit
{
    /// <summary>
    /// Skip test if running on helix (or a particular helix queue).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SkipOnCIAttribute : Attribute, ITestCondition
    {
        public SkipOnCIAttribute(string issueUrl = "")
        {
            IssueUrl = issueUrl;
        }

        public string IssueUrl { get; }

        public bool IsMet
        {
            get
            {
                return OnCI();
            }
        }

        // Queues that should be skipped on, i.e. "Windows.10.Amd64.ClientRS4.VS2017.Open;OSX.1012.Amd64.Open"
        public string Queues { get; set; }

        public string SkipReason
        {
            get
            {
                return $"This test is skipped on CI";
            }
        }

        public static bool OnCI() => OnHelix() || OnAzdo();
        public static bool OnHelix() => !string.IsNullOrEmpty(GetTargetHelixQueue());
        public static string GetTargetHelixQueue() => Environment.GetEnvironmentVariable("helix");
        public static bool OnAzdo() => !string.IsNullOrEmpty(GetIfOnAzdo());
        public static string GetIfOnAzdo() => Environment.GetEnvironmentVariable("AGENT_OS");
    }
}
