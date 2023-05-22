// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

internal sealed class PathReturningFileSystem : IFileSystem
{
    public void ReadFirstLine(FileInfo file, BufferWriter<char> destination)
    {
        destination.Write(file.FullName);
    }

    public void ReadAll(FileInfo file, BufferWriter<char> destination)
    {
        destination.Write(file.FullName);
    }

    public int Read(FileInfo file, int length, Span<char> destination)
    {
        var min = Math.Min(length, file.FullName.Length);

        for (var i = 0; i < min; i++)
        {
            destination[i] = file.FullName[i];
        }

        return min;
    }
}
