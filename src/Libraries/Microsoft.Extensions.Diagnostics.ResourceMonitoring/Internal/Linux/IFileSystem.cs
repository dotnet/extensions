// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

/// <summary>
/// A helper interface used to mock the IO operation in the tests.
/// </summary>
internal interface IFileSystem
{
    /// <summary>
    /// Reads content from the file.
    /// </summary>
    /// <returns>
    /// Chars written.
    /// </returns>
    int Read(FileInfo file, int length, Span<char> destination);

    /// <summary>
    /// Read all content from a file.
    /// </summary>
    void ReadAll(FileInfo file, BufferWriter<char> destination);

    /// <summary>
    /// Reads first line from the file.
    /// </summary>
    void ReadFirstLine(FileInfo file, BufferWriter<char> destination);
}
