// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

internal sealed class HardcodedValueFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _fileContent;
    private readonly string _fallback;

    public HardcodedValueFileSystem(string fallback)
    {
        _fallback = fallback;
        _fileContent = [];
    }

    public HardcodedValueFileSystem(Dictionary<FileInfo, string> fileContent, string fallback = "")
    {
        _fileContent = fileContent.ToDictionary(static x => x.Key.FullName, static y => y.Value, StringComparer.OrdinalIgnoreCase);
        _fallback = fallback;
    }

    public void ReadFirstLine(FileInfo file, BufferWriter<char> destination)
    {
        if (_fileContent.Count == 0 || !_fileContent.TryGetValue(file.FullName, out var content))
        {
            destination.Write(_fallback);

            return;
        }

        var newLineIndex = content.IndexOf('\n');

        destination.Write(newLineIndex != -1 ? content.Substring(0, newLineIndex) : content);
    }

    public void ReadAll(FileInfo file, BufferWriter<char> destination)
    {
        if (_fileContent.Count == 0 || !_fileContent.TryGetValue(file.FullName, out var content))
        {
            destination.Write(_fallback);

            return;
        }

        destination.Write(content);
    }

    public int Read(FileInfo file, int length, Span<char> destination)
    {
        var toRead = _fallback;

        if (_fileContent.Count != 0 && _fileContent.TryGetValue(file.FullName, out var content))
        {
            toRead = content;
        }

        var min = Math.Min(toRead.Length, length);

        for (var i = 0; i < min; i++)
        {
            destination[i] = toRead[i];
        }

        return min;
    }

    public void ReplaceFileContent(FileInfo file, string value)
    {
        _fileContent[file.FullName] = value;
    }
}
