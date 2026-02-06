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
    public async Task<HostedFile> UploadAsync(
        Stream content,
        string? mediaType = null,
        string? fileName = null,
        HostedFileUploadOptions? options = null,
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
            using StreamContent fileContent = new(content);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            multipart.Add(fileContent, "file", fileName);

            using var binaryContent = new HttpContentBinaryContent(multipart);

            var result = await GetContainerClient().CreateContainerFileAsync(
                containerId,
                binaryContent,
                multipart.Headers.ContentType!.ToString(),
                new RequestOptions
                {
                    CancellationToken = cancellationToken
                }).ConfigureAwait(false);

            using var responseDoc = JsonDocument.Parse(result.GetRawResponse().Content);
            var root = responseDoc.RootElement;

            string fileId = root.GetProperty("id").GetString()!;
            string? path = root.TryGetProperty("path", out var pathProp) ? pathProp.GetString() : null;
            string name = path is not null ? Path.GetFileName(path) : fileName;

            return new HostedFile(fileId)
            {
                Name = name,
                MediaType = MediaTypeMap.GetMediaType(name),
            };
        }
        else
        {
            var purpose = options?.Purpose switch
            {
                "assistants" or null => FileUploadPurpose.Assistants,
                "fine-tune" => FileUploadPurpose.FineTune,
                "batch" => FileUploadPurpose.Batch,
                "vision" => FileUploadPurpose.Vision,
                _ => new FileUploadPurpose(options.Purpose)
            };

            var result = await GetFileClient().UploadFileAsync(content, fileName, purpose, cancellationToken).ConfigureAwait(false);

            return ToHostedFile(result.Value);
        }
    }

    /// <inheritdoc />
    public async Task<HostedFileDownloadStream> DownloadAsync(
        string fileId,
        HostedFileDownloadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrWhitespace(fileId);

        if (ResolveScope(options) is string containerId)
        {
            var containerClient = GetContainerClient();
            var containerResult = await containerClient.DownloadContainerFileAsync(containerId, fileId, cancellationToken).ConfigureAwait(false);
            var containerFileInfo = await containerClient.GetContainerFileAsync(containerId, fileId, cancellationToken).ConfigureAwait(false);

            string containerFileName = Path.GetFileName(containerFileInfo.Value.Path);
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
    public async Task<HostedFile?> GetFileInfoAsync(
        string fileId,
        HostedFileGetOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNullOrWhitespace(fileId);

        try
        {
            if (ResolveScope(options) is string containerId)
            {
                var containerResult = await GetContainerClient().GetContainerFileAsync(
                    containerId, fileId, cancellationToken).ConfigureAwait(false);

                var containerFile = containerResult.Value;
                string name = Path.GetFileName(containerFile.Path);

                return new HostedFile(containerFile.Id)
                {
                    Name = name,
                    MediaType = MediaTypeMap.GetMediaType(name),
                    RawRepresentation = containerFile
                };
            }
            else
            {
                var result = await GetFileClient().GetFileAsync(fileId, cancellationToken).ConfigureAwait(false);
                return ToHostedFile(result.Value);
            }
        }
        catch (Exception ex) when (IsNotFoundError(ex))
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<HostedFile> ListFilesAsync(
        HostedFileListOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int limit = options?.Limit ?? int.MaxValue;

        if (ResolveScope(options) is string containerId)
        {
            int count = 0;
            await foreach (var containerFile in GetContainerClient().GetContainerFilesAsync(containerId, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (count >= limit)
                {
                    yield break;
                }

                string name = Path.GetFileName(containerFile.Path);

                yield return new HostedFile(containerFile.Id)
                {
                    Name = name,
                    MediaType = MediaTypeMap.GetMediaType(name),
                    RawRepresentation = containerFile
                };

                count++;
            }
        }
        else
        {
            FilePurpose? purpose = options?.Purpose switch
            {
                "assistants" => FilePurpose.Assistants,
                "fine-tune" => FilePurpose.FineTune,
                "batch" => FilePurpose.Batch,
                "vision" => FilePurpose.Vision,
                _ => null
            };

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

                yield return ToHostedFile(file);
                count++;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(
        string fileId,
        HostedFileDeleteOptions? options = null,
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

    private static HostedFile ToHostedFile(OpenAIFile openAIFile) =>
        new(openAIFile.Id)
        {
            Name = openAIFile.Filename,
            SizeInBytes = openAIFile.SizeInBytes,
            CreatedAt = openAIFile.CreatedAt,
            Purpose = openAIFile.Purpose.ToString(),
            MediaType = MediaTypeMap.GetMediaType(openAIFile.Filename),
            RawRepresentation = openAIFile,
        };

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
}
