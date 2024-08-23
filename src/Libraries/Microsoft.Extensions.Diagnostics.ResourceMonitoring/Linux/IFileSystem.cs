// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

/// <summary>
/// A helper interface used to mock the IO operation in the tests.
/// </summary>
internal interface IFileSystem
{
    /// <summary>
    /// Checks for file existence.
    /// </summary>
    /// <returns><see langword="true"/> if file exists; otherwise, <see langword="false"/>.</returns>
    bool Exists(FileInfo fileInfo);

    /// <summary>
    /// Get directory names on the filesystem based on the specified pattern.
    /// </summary>
    /// <returns> A read-only collection of the paths of directories that match the specified pattern, or an empty read-only collection if no directories are found.</returns>
    IReadOnlyCollection<string> GetDirectoryNames(string directory, string pattern);

    /// <summary>
    /// Reads content of the given length from a file and writes the data in the destination buffer.
    /// </summary>
    /// <returns>
    /// The total number of bytes read into the destination buffer.
    /// </returns>
    int Read(FileInfo file, int length, Span<char> destination);

    /// <summary>
    /// Read all content from a file and writes the data in the destination buffer.
    /// </summary>
    void ReadAll(FileInfo file, BufferWriter<char> destination);

    /// <summary>
    /// Reads first line from the file and writes the data in the destination buffer.
    /// </summary>
    void ReadFirstLine(FileInfo file, BufferWriter<char> destination);

    /// <summary>
    /// Reads all content from a file line by line.
    /// </summary>
    /// <returns>
    /// The enumerable that represents all the lines of the file.
    /// </returns>
    IEnumerable<ReadOnlyMemory<char>> ReadAllByLines(FileInfo file, BufferWriter<char> destination);
}
