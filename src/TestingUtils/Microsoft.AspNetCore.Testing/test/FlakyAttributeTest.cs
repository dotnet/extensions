using Microsoft.AspNetCore.Testing.xunit;
using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Tests
{
    public class FlakyAttributeTest
    {
        [Fact]
        [Flaky("http://example.com")]
        public void AlwaysFlaky()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER")))
            {
                throw new Exception("Flaky!");
            }
        }

        [Fact]
        [Flaky("http://example.com", OnAzDO = false)]
        public void FlakyInHelixOnly()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")))
            {
                throw new Exception("Flaky on Helix!");
            }
        }

        [Fact]
        [Flaky("http://example.com", OnAzDO = false, OnHelixQueues = HelixQueues.macOS1012Amd64 + HelixQueues.Fedora28Amd64)]
        public void FlakyInSpecificHelixQueue()
        {
            var queueName = Environment.GetEnvironmentVariable("HELIX");
            if (!string.IsNullOrEmpty(queueName))
            {

                // Normalize the queue name to have a trailing ';' (this is only for testing anyway)
                if (!queueName.EndsWith(";"))
                {
                    queueName = $"{queueName};";
                }

                var failingQueues = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { HelixQueues.macOS1012Amd64, HelixQueues.Fedora28Amd64 };
                if (failingQueues.Contains(queueName))
                {
                    throw new Exception("Flaky on Helix!");
                }
            }
        }

        [Fact]
        [Flaky("http://example.com", OnHelixQueues = HelixQueues.None)]
        public void FlakyInAzDoOnly()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER")))
            {
                throw new Exception("Flaky on AzDO!");
            }
        }
    }
}
