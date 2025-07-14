// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.AmbientMetadata.Internal;

internal sealed class AzureVmMetadataProvider : IAzureVmMetadataProvider
{
    internal readonly TimeProvider Clock = TimeProvider.System;

    private const int MaxAttempts = 3;
    private const double Timeout = 500;

    private static readonly AzureVmMetadata _emptyMetadata = new();

    private readonly Uri _metadataUri = new("http://169.254.169.254/metadata/instance/compute?api-version=2023-07-01");
    private readonly HttpClient _httpClient;

    public AzureVmMetadataProvider(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Metadata", "true");
    }

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Do need to catch all exceptions.")]
    public async Task<AzureVmMetadata> GetMetadataAsync(CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        try
        {
            for (int i = 0; i < MaxAttempts; i++)
            {
                try
                {
                    response = await _httpClient.GetAsync(_metadataUri, cancellationToken).ConfigureAwait(false);
                    break;
                }
                catch
                {
#if NET
#pragma warning disable EA0002 // the analyzer doesn't understand that TimeProvider is actually used here
                    await Task.Delay(TimeSpan.FromMilliseconds(i * Timeout), Clock, cancellationToken).ConfigureAwait(false);
#pragma warning restore EA0002
#else
                    await Clock.Delay(TimeSpan.FromMilliseconds(i * Timeout), cancellationToken).ConfigureAwait(false);
#endif
                }
            }

            if (response is null)
            {
                return _emptyMetadata;
            }
#if NET
            System.IO.Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            System.IO.Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            return await JsonSerializer.DeserializeAsync(responseStream, SerializerContext.Default.AzureVmMetadata, cancellationToken).ConfigureAwait(false) ?? _emptyMetadata;
        }
        catch
        {
            return _emptyMetadata;
        }
        finally
        {
            response?.Dispose();
        }
    }
}
