using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    [TraitDiscoverer("Microsoft.AspNetCore.Testing.xunit.FlakyTestDiscoverer", "Microsoft.AspNetCore.Testing")]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FlakyAttribute : Attribute, ITraitAttribute
    {
        private List<string> _helixQueues = new List<string>() { HelixQueues.All };
        private List<string> _azurePipelinesOSes = new List<string>() { AzurePipelines.All };

        /// <summary>
        /// Gets a URL to a GitHub issue tracking this flaky test.
        /// </summary>
        public string GitHubIssueUrl { get; }

        /// <summary>
        /// Gets or sets a list of helix queues on which this test is flaky. Defaults to <see cref="HelixQueues.All"/> indicating it is flaky on all Helix queues. See
        /// <see cref="HelixQueues"/> for a list of valid values.
        /// </summary>
        public string OnHelix
        {
            get => string.Join(";", _helixQueues);
            set => _helixQueues = new List<string>(value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Gets or sets a list of Azure Pipelines (AzP) operating systems on which this test is flaky. Defaults to <see cref="AzurePipelines.All"/> indicating it is flaky on all AzP OSes.
        /// See <see cref="AzurePipelines"/> for a list of valid values.
        /// </summary>
        public string OnAzP
        {
            get => string.Join(";", _azurePipelinesOSes);
            set => _azurePipelinesOSes = new List<string>(value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Gets a list of Helix queues on which the test is flaky (including <see cref="HelixQueues.All"/> if specified).
        /// </summary>
        public IReadOnlyList<string> FlakyHelixQueues => _helixQueues;

        /// <summary>
        /// Gets a list of Azure Pipelines operating systems on which the test is flaky (including <see cref="HelixQueues.All"/> if specified).
        /// </summary>
        public IReadOnlyList<string> FlakyAzurePipelinesOSes => _azurePipelinesOSes;

        public FlakyAttribute(string gitHubIssueUrl)
        {
            GitHubIssueUrl = gitHubIssueUrl;
        }
    }
}
