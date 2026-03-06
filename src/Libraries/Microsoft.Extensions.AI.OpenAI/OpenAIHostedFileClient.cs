// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Containers;
using OpenAI.Files;

#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable IDE0058 // Expression value is never used

namespace Microsoft.Extensions.AI;

/// <summary>
/// An <see cref="IHostedFileClient"/> implementation for OpenAI file operations.
/// </summary>
/// <remarks>
/// <para>
/// This client supports both the standard Files API and container-scoped files (used for code interpreter outputs).
/// When a <see cref="HostedFileClientOptions.Scope"/> (container ID) is specified on a per-call options object
/// or as the default scope at construction time, operations target that container. Otherwise, operations use
/// the standard Files API.
/// </para>
/// <para>
/// Depending on how this client is constructed, it may support only file operations, only container operations,
/// or both. If an operation requires a client that was not provided, an <see cref="InvalidOperationException"/> is thrown.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIFiles, UrlFormat = DiagnosticIds.UrlFormat)]
internal sealed class OpenAIHostedFileClient : IHostedFileClient
{
    /// <summary>The underlying <see cref="OpenAIFileClient"/> for standard file operations, or <see langword="null"/> if not available.</summary>
    private readonly OpenAIFileClient? _fileClient;

    /// <summary>The underlying <see cref="ContainerClient"/> for container file operations, or <see langword="null"/> if not available.</summary>
    private readonly ContainerClient? _containerClient;

    /// <summary>The default scope (container ID) for operations, or <see langword="null"/> if not set.</summary>
    private readonly string? _defaultScope;

    /// <summary>The metadata for this client.</summary>
    private readonly HostedFileClientMetadata _metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIHostedFileClient"/> class from an <see cref="OpenAIClient"/>.
    /// </summary>
    /// <param name="openAIClient">The underlying <see cref="OpenAIClient"/>.</param>
    public OpenAIHostedFileClient(OpenAIClient openAIClient)
    {
        _ = Throw.IfNull(openAIClient);
        _fileClient = openAIClient.GetOpenAIFileClient();
        _containerClient = openAIClient.GetContainerClient();
        _metadata = new HostedFileClientMetadata("openai", _fileClient.Endpoint);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIHostedFileClient"/> class from an <see cref="OpenAIFileClient"/>.
    /// </summary>
    /// <param name="fileClient">The underlying <see cref="OpenAIFileClient"/>.</param>
    public OpenAIHostedFileClient(OpenAIFileClient fileClient)
    {
        _fileClient = Throw.IfNull(fileClient);
        _metadata = new HostedFileClientMetadata("openai", _fileClient.Endpoint);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIHostedFileClient"/> class from a <see cref="ContainerClient"/>.
    /// </summary>
    /// <param name="containerClient">The underlying <see cref="ContainerClient"/>.</param>
    /// <param name="defaultScope">
    /// The default container ID for operations. If not specified, a container ID must be
    /// provided via the <see cref="HostedFileClientOptions.Scope"/> property on per-call options.
    /// </param>
    public OpenAIHostedFileClient(ContainerClient containerClient, string? defaultScope = null)
    {
        _containerClient = Throw.IfNull(containerClient);
        _defaultScope = defaultScope;
        _metadata = new HostedFileClientMetadata("openai", _containerClient.Endpoint);
    }

    /// <inheritdoc />
    public async Task<HostedFileContent> UploadAsync(
        Stream content,
        string? mediaType = null,
        string? fileName = null,
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(content);

        fileName ??= content is FileStream fs ? Path.GetFileName(fs.Name) : null;
        mediaType ??= fileName is not null ? MediaTypeMap.GetMediaType(fileName) : null;
        fileName ??= $"{Guid.NewGuid():N}{MediaTypeMap.GetExtension(mediaType)}";

        if (ResolveScope(options) is string containerId)
        {
            mediaType ??= "application/octet-stream";

            using MultipartFormDataContent multipart = new();
            using NonDisposingStreamContent fileContent = new(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            multipart.Add(fileContent, "file", fileName);

            using var binaryContent = new HttpContentBinaryContent(multipart);

            var requestOptions = options?.RawRepresentationFactory?.Invoke(this) as RequestOptions ?? new();
            requestOptions.CancellationToken = cancellationToken;

            var result = await GetContainerClient().CreateContainerFileAsync(
                containerId,
                binaryContent,
                multipart.Headers.ContentType!.ToString(),
                requestOptions).ConfigureAwait(false);

            using var responseDoc = JsonDocument.Parse(result.GetRawResponse().Content);
            return ParseContainerFileJson(responseDoc.RootElement, containerId)
                ?? throw new InvalidOperationException("The container file upload response did not include a valid file ID.");
        }
        else
        {
            var purpose =
                options?.Purpose is null ? FileUploadPurpose.UserData :
                string.Equals("assistants", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FileUploadPurpose.Assistants :
                string.Equals("batch", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FileUploadPurpose.Batch :
                string.Equals("evaluations", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FileUploadPurpose.Evaluations :
                string.Equals("fine-tune", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FileUploadPurpose.FineTune :
                string.Equals("user_data", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FileUploadPurpose.UserData :
                string.Equals("vision", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FileUploadPurpose.Vision :
                new FileUploadPurpose(options.Purpose);

            var result = await GetFileClient().UploadFileAsync(content, fileName, purpose, cancellationToken).ConfigureAwait(false);

            return ToHostedFileContent(result.Value);
        }
    }

    /// <inheritdoc />
    public async Task<HostedFileDownloadStream> DownloadAsync(
        string fileId,
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrWhitespace(fileId);

        if (ResolveScope(options) is string containerId)
        {
            var containerClient = GetContainerClient();
            var containerResult = await containerClient.DownloadContainerFileAsync(containerId, fileId, cancellationToken).ConfigureAwait(false);

            // Use protocol method to get file metadata as raw JSON. This works around
            // https://github.com/openai/openai-dotnet/issues/733, where the SDK's typed
            // deserialization crashes on container files with a null "bytes" value.
            var containerFileInfoResult = await containerClient.GetContainerFileAsync(
                containerId, fileId, new RequestOptions { CancellationToken = cancellationToken }).ConfigureAwait(false);
            using var infoDoc = JsonDocument.Parse(containerFileInfoResult.GetRawResponse().Content);
            string? path = infoDoc.RootElement.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : null;
            string containerFileName = path is not null ? Path.GetFileName(path) : fileId;
            string? containerMediaType = MediaTypeMap.GetMediaType(containerFileName) ?? "application/octet-stream";

            return new OpenAIFileDownloadStream(containerResult.Value, containerMediaType, containerFileName);
        }
        else
        {
            var fileClient = GetFileClient();
            var result = await fileClient.DownloadFileAsync(fileId, cancellationToken).ConfigureAwait(false);
            var fileInfo = await fileClient.GetFileAsync(fileId, cancellationToken).ConfigureAwait(false);

            string? mediaType = MediaTypeMap.GetMediaType(fileInfo.Value.Filename) ?? "application/octet-stream";

            return new OpenAIFileDownloadStream(result.Value, mediaType, fileInfo.Value.Filename);
        }
    }

    /// <inheritdoc />
    public async Task<HostedFileContent?> GetFileInfoAsync(
        string fileId,
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrWhitespace(fileId);

        try
        {
            if (ResolveScope(options) is string containerId)
            {
                // Use protocol method to get file metadata as raw JSON. This works around
                // https://github.com/openai/openai-dotnet/issues/733, where the SDK's typed
                // deserialization crashes on container files with a null "bytes" value.
                var containerResult = await GetContainerClient().GetContainerFileAsync(
                    containerId, fileId, new RequestOptions { CancellationToken = cancellationToken }).ConfigureAwait(false);

                using var doc = JsonDocument.Parse(containerResult.GetRawResponse().Content);
                return ParseContainerFileJson(doc.RootElement, containerId);
            }
            else
            {
                var result = await GetFileClient().GetFileAsync(fileId, cancellationToken).ConfigureAwait(false);
                return ToHostedFileContent(result.Value);
            }
        }
        catch (Exception ex) when (IsNotFoundError(ex))
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<HostedFileContent> ListFilesAsync(
        HostedFileClientOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int limit = options?.Limit ?? int.MaxValue;

        if (ResolveScope(options) is string containerId)
        {
            // Use OpenAI's protocol overload to make single-page requests, handling paging manually.
            // This works around https://github.com/openai/openai-dotnet/issues/733, where both
            // the convenience and protocol collection overloads crash during auto-pagination when
            // deserializing container files with a null "bytes" value. By only taking the first raw
            // page from each request, the SDK's internal deserialization for pagination is never triggered.
            var containerClient = GetContainerClient();

            int count = 0;
            string? after = null;

            while (true)
            {
                AsyncCollectionResult result = containerClient.GetContainerFilesAsync(
                    containerId, limit < int.MaxValue ? limit : null,
                    null, after, new() { CancellationToken = cancellationToken });

                // Get only the first raw page. We must not let the SDK auto-paginate
                // because its pagination logic deserializes the full response, which crashes.
                IAsyncEnumerator<ClientResult> pages = result.GetRawPagesAsync().GetAsyncEnumerator(cancellationToken);
                JsonDocument doc;
                try
                {
                    if (!await pages.MoveNextAsync().ConfigureAwait(false))
                    {
                        break;
                    }

                    doc = JsonDocument.Parse(pages.Current.GetRawResponse().Content);
                }
                finally
                {
                    await pages.DisposeAsync().ConfigureAwait(false);
                }

                using (doc)
                {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("data", out JsonElement data) && data.ValueKind is JsonValueKind.Array)
                    {
                        foreach (var fileElement in data.EnumerateArray())
                        {
                            if (count >= limit)
                            {
                                yield break;
                            }

                            var file = ParseContainerFileJson(fileElement, containerId);
                            if (file is null)
                            {
                                continue;
                            }

                            yield return file;
                            count++;
                        }
                    }

                    bool hasMore = root.TryGetProperty("has_more", out var hm) && hm.ValueKind is JsonValueKind.True;
                    string? lastId = root.TryGetProperty("last_id", out var li) ? li.GetString() : null;
                    if (!hasMore || string.IsNullOrEmpty(lastId))
                    {
                        break;
                    }

                    after = lastId;
                }
            }
        }
        else
        {
            var purpose =
                options?.Purpose is null ? FilePurpose.UserData :
                string.Equals("assistants", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FilePurpose.Assistants :
                string.Equals("assistants_output", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FilePurpose.AssistantsOutput :
                string.Equals("batch", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FilePurpose.Batch :
                string.Equals("batch_output", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FilePurpose.BatchOutput :
                string.Equals("fine-tune", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FilePurpose.FineTune :
                string.Equals("fine-tune-results", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FilePurpose.FineTuneResults :
                string.Equals("vision", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FilePurpose.Vision :
                string.Equals("evaluations", options.Purpose, StringComparison.OrdinalIgnoreCase) ? FilePurpose.Evaluations :
                FilePurpose.UserData;

            var fileClient = GetFileClient();
            var result = await (purpose is FilePurpose p ?
                fileClient.GetFilesAsync(p, cancellationToken) :
                fileClient.GetFilesAsync(cancellationToken)).ConfigureAwait(false);

            int count = 0;
            foreach (var file in result.Value)
            {
                if (count >= limit)
                {
                    yield break;
                }

                yield return ToHostedFileContent(file);
                count++;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string fileId,
        HostedFileClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrWhitespace(fileId);

        try
        {
            if (ResolveScope(options) is string containerId)
            {
                await GetContainerClient().DeleteContainerFileAsync(containerId, fileId, cancellationToken).ConfigureAwait(false);
                return true;
            }
            else
            {
                var result = await GetFileClient().DeleteFileAsync(fileId, cancellationToken).ConfigureAwait(false);
                return result.Value.Deleted;
            }
        }
        catch (Exception ex) when (IsNotFoundError(ex))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(HostedFileClientMetadata) ? _metadata :
            serviceType == typeof(OpenAIFileClient) ? _fileClient :
            serviceType == typeof(ContainerClient) ? _containerClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose; the underlying clients are not owned by this instance.
    }

    private static HostedFileContent ToHostedFileContent(OpenAIFile openAIFile) =>
        new(openAIFile.Id)
        {
            Name = openAIFile.Filename,
            SizeInBytes = openAIFile.SizeInBytes,
            CreatedAt = openAIFile.CreatedAt,
            Purpose = openAIFile.Purpose.ToString(),
            MediaType = MediaTypeMap.GetMediaType(openAIFile.Filename),
            RawRepresentation = openAIFile,
        };

    /// <summary>
    /// Parses container file metadata from a JSON element into a <see cref="HostedFileContent"/>.
    /// </summary>
    /// <remarks>
    /// This parses raw JSON rather than using the OpenAI SDK's typed deserialization,
    /// as a workaround for <see href="https://github.com/openai/openai-dotnet/issues/733"/>,
    /// where the SDK crashes deserializing container files when the "bytes" field is null.
    /// Once the SDK issue is fixed, call sites should revert to using the typed API.
    /// </remarks>
    private static HostedFileContent? ParseContainerFileJson(JsonElement element, string? scope)
    {
        if (!element.TryGetProperty("id", out var idProp) || idProp.GetString() is not { } id)
        {
            return null;
        }

        string? path = element.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : null;
        string name = path is not null ? Path.GetFileName(path) : id;

        long? sizeInBytes = element.TryGetProperty("bytes", out var bytesProp) && bytesProp.ValueKind is JsonValueKind.Number
            ? bytesProp.GetInt64()
            : null;

        DateTimeOffset? createdAt = element.TryGetProperty("created_at", out var createdProp) && createdProp.ValueKind is JsonValueKind.Number
            ? DateTimeOffset.FromUnixTimeSeconds(createdProp.GetInt64())
            : null;

        return new HostedFileContent(id)
        {
            Name = name,
            MediaType = MediaTypeMap.GetMediaType(name),
            SizeInBytes = sizeInBytes,
            CreatedAt = createdAt,
            Scope = scope,
        };
    }

    private static bool IsNotFoundError(Exception ex) =>
        ex is ClientResultException { Status: 404 };

    private OpenAIFileClient GetFileClient() =>
        _fileClient ??
        throw new InvalidOperationException(
            $"This operation requires the standard Files API, but this client was not constructed with an {nameof(OpenAIFileClient)}. " +
            $"Use an {nameof(IHostedFileClient)} created from an {nameof(OpenAIClient)} or {nameof(OpenAIFileClient)}, or set the Scope option to target a container instead.");

    private ContainerClient GetContainerClient() =>
        _containerClient ??
        throw new InvalidOperationException(
            $"This operation requires a container (Scope was specified), but this client was not constructed with a {nameof(ContainerClient)}. " +
            $"Use an {nameof(IHostedFileClient)} created from an {nameof(OpenAIClient)} or {nameof(ContainerClient)} to access container files.");

    /// <summary>Resolves the scope (container ID) from per-call options or the default.</summary>
    private string? ResolveScope(HostedFileClientOptions? options) =>
        options?.Scope ?? _defaultScope;

    /// <summary>A <see cref="BinaryContent"/> that writes an <see cref="HttpContent"/> directly to the output stream.</summary>
    private sealed class HttpContentBinaryContent(HttpContent httpContent) : BinaryContent
    {
        public override void WriteTo(Stream stream, CancellationToken cancellationToken = default)
        {
#if NET
            httpContent.CopyTo(stream, null, cancellationToken);
#else
#pragma warning disable VSTHRD002 // Synchronously waiting - no sync CopyTo on older TFMs
            httpContent.CopyToAsync(stream).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
#endif
        }

        public override Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default) =>
#if NET
            httpContent.CopyToAsync(stream, cancellationToken);
#else
            httpContent.CopyToAsync(stream);
#endif

        public override bool TryComputeLength(out long length)
        {
            length = httpContent.Headers.ContentLength.GetValueOrDefault(-1);
            return length >= 0;
        }

        public override void Dispose()
        {
        }
    }

    /// <summary>A <see cref="StreamContent"/> that does not dispose the underlying stream.</summary>
    private sealed class NonDisposingStreamContent(Stream stream) : StreamContent(stream)
    {
#pragma warning disable CA2215 // Intentionally not calling base.Dispose to avoid disposing the caller's stream
        protected override void Dispose(bool disposing)
        {
            // Do not call base.Dispose; it would dispose the caller's stream.
        }
#pragma warning restore CA2215
    }
}
