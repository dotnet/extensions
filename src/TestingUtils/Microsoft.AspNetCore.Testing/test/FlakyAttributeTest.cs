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
            throw new Exception("Flakey!");
        }

        [Fact]
        [Flaky("http://example.com", OnAzDO = false)]
        public void FlakyInHelixOnly()
        {
            // TODO: Use actual Helix detection variable ;)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX")))
            {
                throw new Exception("Flaky on Helix!");
            }
        }

        [Fact]
        [Flaky("http://example.com", OnAzDO = false, OnHelixQueues = HelixQueues.macOS1012 + HelixQueues.Fedora28)]
        public void FlakyInSpecificHelixQueue()
        {
            // TODO: Use actual Helix detection variable ;)
            var queueName = Environment.GetEnvironmentVariable("HELIX");
            if (!string.IsNullOrEmpty(queueName))
            {

                // Normalize the queue name to have a trailing ';' (this is only for testing anyway)
                if (!queueName.EndsWith(";"))
                {
                    queueName = $"{queueName};";
                }

                var failingQueues = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { HelixQueues.macOS1012, HelixQueues.Fedora28 };
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
            // TODO: Use actual AzDO detection variable ;)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZDO")))
            {
                throw new Exception("Flaky on AzDO!");
            }
        }
    }
}
