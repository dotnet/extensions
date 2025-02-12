// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Html;

/// <summary>
/// An <see cref="IEvaluationReportWriter"/> that generates an HTML report containing all the
/// <see cref="EvaluationMetric"/>s present in the supplied <see cref="ScenarioRunResult"/>s and writes it to the
/// specified <paramref name="reportFilePath"/>.
/// </summary>
/// <param name="reportFilePath">
/// The path to a file where the report will be written. If the file already exists, it will be overwritten.
/// </param>
public sealed class HtmlReportWriter(string reportFilePath) : IEvaluationReportWriter
{
    /// <inheritdoc/>
    public async ValueTask WriteReportAsync(
        IEnumerable<ScenarioRunResult> scenarioRunResults,
        CancellationToken cancellationToken = default)
    {
        var dataset =
            new Dataset(
                scenarioRunResults.ToList(),
                createdAt: DateTime.UtcNow,
                generatorVersion: Constants.Version);

        using var stream =
            new FileStream(
                reportFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                useAsync: true);

        using var writer = new StreamWriter(stream, Encoding.UTF8);

#if NET
        await writer.WriteAsync(HtmlTemplateBefore.AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
#else
        await writer.WriteAsync(HtmlTemplateBefore).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
#endif

        await JsonSerializer.SerializeAsync(
            stream,
            dataset,
            SerializerContext.Compact.Dataset,
            cancellationToken).ConfigureAwait(false);

#if NET
        await writer.WriteAsync(HtmlTemplateAfter.AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
#else
        await writer.WriteAsync(HtmlTemplateAfter).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
#endif
    }

    private static string HtmlTemplateBefore { get; }
    private static string HtmlTemplateAfter { get; }

#pragma warning disable CA1065, S3877
    // CA1065, S3877: Do not raise exceptions in static constructors.
    // We disable this warning because the exception is only thrown in catastrophic circumstances where we somehow
    // failed to include the html templates in the assembly as part of the build process. This is highly unlikely to
    // happen in practice. If this does happen somehow, it is better to fail fast and loudly.
    static HtmlReportWriter()
    {
        using Stream resourceStream =
            typeof(HtmlReportWriter).Assembly.GetManifestResourceStream("Reporting.HTML.index.html")
                ?? throw new InvalidOperationException("Failed to load HTML template.");

        // TASK: Make this more efficient by scanning the stream rather than reading it all into memory.
        using var reader = new StreamReader(resourceStream);
        string all = reader.ReadToEnd();

        // This is the placeholder for the results array in the template.
        const string SearchString = @"{scenarioRunResults:[]}";

        int start = all.IndexOf(SearchString, StringComparison.Ordinal);
        if (start == -1)
        {
            throw new InvalidOperationException($"Placeholder '{SearchString}' not found in the HTML template.");
        }

        HtmlTemplateBefore = all.Substring(0, start);
        HtmlTemplateAfter = all.Substring(start + SearchString.Length);
    }
#pragma warning restore CA1065
}
