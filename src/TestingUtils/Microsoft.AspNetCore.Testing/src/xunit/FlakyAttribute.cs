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
        private List<string> _azPJobs = new List<string>() { AzurePipelines.All };

        /// <summary>
        /// Gets a URL to a GitHub issue tracking this flaky test.
        /// </summary>
        public string GitHubIssueUrl { get; }

        /// <summary>
        /// Gets or sets a list of helix queues on which this test is flaky. Defaults to <see cref="HelixQueues.All"/> indicating it is flaky on all Helix queues. See
        /// <see cref="HelixQueues"/> for a list of valid values.
        /// </summary>
        public string OnHelixQueues
        {
            get => string.Join(";", _helixQueues);
            set => _helixQueues = new List<string>(value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Gets or sets a boolean indicating if this test is flaky on Azure DevOps Pipelines. Defaults to <see langword="true" />.
        /// </summary>
        public string OnAzPJobs
        {
            get => string.Join(";", _azPJobs);
            set => _azPJobs = new List<string>(value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Gets a list of Helix queues on which the test is flaky (including <see cref="HelixQueues.All"/> if specified).
        /// </summary>
        public IReadOnlyList<string> FlakyHelixQueues => _helixQueues;

        /// <summary>
        /// Gets a list of Azure Pipelines jobs on which the test is flaky (including <see cref="HelixQueues.All"/> if specified).
        /// </summary>
        public IReadOnlyList<string> FlakyAzPJobs => _helixQueues;

        public FlakyAttribute(string gitHubIssueUrl)
        {
            GitHubIssueUrl = gitHubIssueUrl;
        }
    }
}
