// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET

#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

/// <summary>
/// Provides polyfill extension members for <see cref="File"/> for older frameworks.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class FilePolyfills
{
    extension(File)
    {
        /// <summary>
        /// Asynchronously reads all bytes from a file.
        /// </summary>
        /// <param name="path">The file to read from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation, which wraps the byte array containing the contents of the file.</returns>
        public static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
            byte[] data = new byte[stream.Length];
            int totalRead = 0;
            while (totalRead < data.Length)
            {
                int read = await stream.ReadAsync(data, totalRead, data.Length - totalRead, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                totalRead += read;
            }

            return data;
        }
    }
}

#endif
