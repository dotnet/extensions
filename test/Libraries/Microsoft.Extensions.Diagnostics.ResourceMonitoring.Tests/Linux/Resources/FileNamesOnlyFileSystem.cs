// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Linq;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

internal sealed class FileNamesOnlyFileSystem : IFileSystem
{
    private readonly string _directory;
    public bool Exists(FileInfo fileInfo)
    {
        return fileInfo.Exists;
    }

    public FileNamesOnlyFileSystem(string directory)
    {
        _directory = directory;
    }

    public void ReadFirstLine(FileInfo file, BufferWriter<char> destination)
    {
        var a = File.ReadAllLines($"{_directory}/{file.Name}")
             .FirstOrDefault() ?? string.Empty;

        destination.Write(a);
    }

    public void ReadAll(FileInfo file, BufferWriter<char> destination)
    {
        var c = File.ReadAllText($"{_directory}/{file.Name}");

        destination.Write(c);
    }

    public int Read(FileInfo file, int length, Span<char> destination)
    {
        var c = File.ReadAllText($"{_directory}/{file.Name}");
        var min = Math.Min(length, c.Length);

        for (var i = 0; i < min; i++)
        {
            destination[i] = c[i];
        }

        return min;
    }
}
