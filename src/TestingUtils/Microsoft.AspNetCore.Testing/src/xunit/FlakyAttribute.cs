using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing.xunit
{
    /// <summary>
    /// Marks a test as "Flaky" so that the build will sequester it and ignore failures.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute works by applying xUnit.net "Traits" based on the criteria specified in the attribute
    /// properties. Once these traits are applied, build scripts can include/exclude tests based on them.
    /// </para>
    /// <para>
    /// All flakiness-related traits start with <code>Flaky:</code> and are grouped first by the process running the tests: Azure Pipelines (AzP) or Helix.
    /// Then there is a segment specifying the "selector" which indicates where the test is flaky. Finally a segment specifying the value of that selector.
    /// The value of these traits is always either "true" or the trait is not present. We encode the entire selector in the name of the trait because xUnit.net only
    /// provides "==" and "!=" operators for traits, there is no way to check if a trait "contains" or "does not contain" a value. VSTest does support "contains" checks
    /// but does not appear to support "does not contain" checks. Using this pattern means we can use simple "==" and "!=" checks to either only run flaky tests, or exclude
    /// flaky tests.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Fact]
    /// [Flaky(OnHelix = HelixQueues.Fedora28Amd64, OnAzP = AzurePipelines.macOS)]
    /// public void FlakyTest()
    /// {
    ///     // Flakiness
    /// }
    /// </code>
    ///
    /// <para>
    /// The above example generates the following facets:
    /// </para>
    ///
    /// <list type="bullet">
    /// <item>
    ///     <description><c>Flaky:Helix:Queue:Fedora.28.Amd64.Open</c> = <c>true</c></description>
    /// </item>
    /// <item>
    ///     <description><c>Flaky:AzP:OS:Darwin</c> = <c>true</c></description>
    /// </item>
    /// </list>
    ///
    /// <para>
    /// Given the above attribute, the Azure Pipelines macOS run can easily filter this test out by passing <c>-notrait "Flaky:AzP:OS:all=true" -notrait "Flaky:AzP:OS:Darwin=true"</c>
    /// to <c>xunit.console.exe</c>. Similarly, it can run only flaky tests using <c>-trait "Flaky:AzP:OS:all=true" -trait "Flaky:AzP:OS:Darwin=true"</c>
    /// </para>
    /// </example>
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
