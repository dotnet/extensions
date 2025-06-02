// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Disk.Test;

internal class FakeDiskStatsReader(Dictionary<string, List<DiskStats>> stats) : IDiskStatsReader
{
    private int _index;

    public List<DiskStats> ReadAll()
    {
        if (_index >= stats.Values.First().Count)
        {
            throw new InvalidOperationException("No more values available.");
        }

        List<DiskStats> list = stats.Values.Select(x => x[_index]).ToList();
        _index++;
        return list;
    }
}
