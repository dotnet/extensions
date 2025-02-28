// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Azure.Storage.Files.DataLake.Specialized;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

/// <summary>
/// An <see cref="IResultStore"/> implementation that stores <see cref="ScenarioRunResult"/>s under an Azure Storage
/// container.
/// </summary>
/// <param name="client">
/// A <see cref="DataLakeDirectoryClient"/> with access to an Azure Storage container under which the
/// <see cref="ScenarioRunResult"/>s should be stored.
/// </param>
public sealed class AzureStorageResultStore(DataLakeDirectoryClient client) : IResultStore
{
    private const string ResultsRootPrefix = "results";

    private const string DeserializationFailedMessage = "Unable to deserialize the scenario run result file at {0}.";

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GetLatestExecutionNamesAsync(
        int? count = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int remaining = count ?? 1;

        (string path, _) = GetResultPath();
        DataLakeDirectoryClient subClient = client.GetSubDirectoryClient(path);

#pragma warning disable S3254 // Default parameter value (for 'recursive') should not be passed as argument.
        await foreach (PathItem item in
            subClient.GetPathsAsync(recursive: false, cancellationToken: cancellationToken).ConfigureAwait(false))
#pragma warning restore S3254
        {
            if (remaining > 0)
            {
                yield return GetLastSegmentFromPath(item.Name);
                remaining--;
            }
            else
            {
                break;
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GetScenarioNamesAsync(
        string executionName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (string path, _) = GetResultPath(executionName);
        DataLakeDirectoryClient subClient = client.GetSubDirectoryClient(path);

#pragma warning disable S3254 // Default parameter value (for 'recursive') should not be passed as argument.
        await foreach (PathItem item in
            subClient.GetPathsAsync(recursive: false, cancellationToken: cancellationToken).ConfigureAwait(false))
#pragma warning restore S3254
        {
            yield return GetLastSegmentFromPath(item.Name);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> GetIterationNamesAsync(
        string executionName,
        string scenarioName,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (string path, _) = GetResultPath(executionName, scenarioName);
        DataLakeDirectoryClient subClient = client.GetSubDirectoryClient(path);

#pragma warning disable S3254 // Default parameter value (for 'recursive') should not be passed as argument.
        await foreach (PathItem item in
            subClient.GetPathsAsync(recursive: false, cancellationToken: cancellationToken).ConfigureAwait(false))
#pragma warning restore S3254
        {
            yield return StripExtension(GetLastSegmentFromPath(item.Name));
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ScenarioRunResult> ReadResultsAsync(
        string? executionName = null,
        string? scenarioName = null,
        string? iterationName = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (string path, _) = GetResultPath(executionName, scenarioName, iterationName);
        DataLakeDirectoryClient subClient = client.GetSubDirectoryClient(path);

        await foreach (PathItem pathItem in
            subClient.GetPathsAsync(recursive: true, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (pathItem.IsDirectory ?? true)
            {
                continue;
            }

            DataLakeFileClient fileClient = client.GetParentFileSystemClient().GetFileClient(pathItem.Name);

            Response<DataLakeFileReadResult> content =
                await fileClient.ReadContentAsync(cancellationToken).ConfigureAwait(false);

            ScenarioRunResult? result = await JsonSerializer.DeserializeAsync(
                content.Value.Content.ToStream(),
                AzureStorageSerializerContext.Default.ScenarioRunResult,
                cancellationToken).ConfigureAwait(false)
                    ?? throw new JsonException(
                        string.Format(CultureInfo.CurrentCulture, DeserializationFailedMessage, fileClient.Name));

            yield return result;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DeleteResultsAsync(
        string? executionName = null,
        string? scenarioName = null,
        string? iterationName = null,
        CancellationToken cancellationToken = default)
    {
        (string path, bool isDir) = GetResultPath(executionName, scenarioName, iterationName);

        if (isDir)
        {
            _ = await client
                    .GetSubDirectoryClient(path)
                    .DeleteIfExistsAsync(recursive: true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _ = await client
                    .GetFileClient(path)
                    .DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask WriteResultsAsync(
        IEnumerable<ScenarioRunResult> results,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(results, nameof(results));

        foreach (ScenarioRunResult result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            (string path, _) = GetResultPath(result.ExecutionName, result.ScenarioName, result.IterationName);

            DataLakeFileClient fileClient = client.GetFileClient(path);

            MemoryStream stream = new();

            await JsonSerializer.SerializeAsync(
                stream,
                result,
                AzureStorageSerializerContext.Default.ScenarioRunResult,
                cancellationToken).ConfigureAwait(false);

            _ = stream.Seek(0, SeekOrigin.Begin);

            _ = await fileClient.UploadAsync(
                    stream,
                    overwrite: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private static string GetLastSegmentFromPath(string name)
        => name.Substring(name.LastIndexOf('/') + 1);

    private static string StripExtension(string name)
        => name.Substring(0, name.LastIndexOf('.'));

    private static (string path, bool isDir) GetResultPath(
        string? executionName = null,
        string? scenarioName = null,
        string? iterationName = null)
    {
        if (executionName is null)
        {
            return ($"{ResultsRootPrefix}/", isDir: true);
        }
        else if (scenarioName is null)
        {
            return ($"{ResultsRootPrefix}/{executionName}/", isDir: true);
        }
        else if (iterationName is null)
        {
            return ($"{ResultsRootPrefix}/{executionName}/{scenarioName}/", isDir: true);
        }

        return ($"{ResultsRootPrefix}/{executionName}/{scenarioName}/{iterationName}.json", isDir: false);
    }
}
