// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Gen.Logging.Test;

public static class ProvidedPropertyTagProvider
{
    public static void RecordTags(ITagCollector collector, ProvidedProperty? property)
    {
        collector.Add("provided.property", property?.Value);
    }
}
