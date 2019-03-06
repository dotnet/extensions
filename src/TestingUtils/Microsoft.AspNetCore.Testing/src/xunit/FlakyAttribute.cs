using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    [TraitDiscoverer("Microsoft.AspNetCore.Testing.xunit.FlakyTestDiscoverer", "Microsoft.AspNetCore.Testing")]
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class FlakyAttribute : Attribute, ITraitAttribute
    {
        private List<string> _queues = new List<string>() { "all" };

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
            get => string.Join(";", _queues);
            set => _queues = new List<string>(value.Split(';'));
        }

        /// <summary>
        /// Gets or sets a boolean indicating if this test is flaky on Azure DevOps Pipelines. Defaults to <see langword="true" />.
        /// </summary>
        public bool OnAzDO { get; set; } = true;

        /// <summary>
        /// Gets a list of Helix queues on which the test is flaky (including <see cref="HelixQueues.All"/> if specified).
        /// </summary>
        public IReadOnlyList<string> FlakyQueues => _queues;

        public FlakyAttribute(string gitHubIssueUrl)
        {
            GitHubIssueUrl = gitHubIssueUrl;
        }
    }
}
