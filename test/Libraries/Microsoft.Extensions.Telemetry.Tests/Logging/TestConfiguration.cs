// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Microsoft.Extensions.Telemetry.Logging.Test;

internal class TestConfiguration : JsonConfigurationProvider
{
    private Func<string> _json;

    public TestConfiguration(JsonConfigurationSource source, Func<string> json)
        : base(source)
    {
        _json = json;
    }

    public static ConfigurationRoot Create(Func<string> getJson)
    {
        var provider = new TestConfiguration(new JsonConfigurationSource { Optional = true }, getJson);
        return new ConfigurationRoot(new List<IConfigurationProvider> { provider });
    }

    public override void Load()
    {
        var stream = new MemoryStream();
        using var writer = new StreamWriter(stream);
        writer.Write(_json());
        writer.Flush();
        stream.Seek(0, SeekOrigin.Begin);
        Load(stream);
    }
}
