﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class DiskBasedResponseCacheTests : ResponseCacheTester, IAsyncLifetime
{
    private readonly List<string> _tempStorage = [];

    private string UseTempStoragePath()
    {
        string path = Path.Combine(Path.GetTempPath(), "M.E.AI.Eval.ResponseCacheTests", Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        _tempStorage.Add(path);
        return path;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        foreach (string path in _tempStorage)
        {
            try
            {
                Directory.Delete(path, true);
            }
#pragma warning disable CA1031 // Do not catch general exception types.
            catch
#pragma warning restore CA1031
            {
                // Best effort delete, don't crash on exceptions.
            }
        }

        return Task.CompletedTask;
    }

    internal override bool IsConfigured => true;

    internal override IResponseCacheProvider CreateResponseCacheProvider()
        => new DiskBasedResponseCacheProvider(UseTempStoragePath());

    internal override IResponseCacheProvider CreateResponseCacheProvider(Func<DateTime> provideDateTime)
        => new DiskBasedResponseCacheProvider(UseTempStoragePath(), provideDateTime);
}
