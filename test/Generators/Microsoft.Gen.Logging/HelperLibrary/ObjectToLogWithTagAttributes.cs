// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Gen.Logging.Test;

public class ObjectToLogWithTagAttributes
{
    [TagName("property.to.log")]
    public string? PropertyToLog { get; set; }

    [TagProvider(typeof(ProvidedPropertyTagProvider), nameof(ProvidedPropertyTagProvider.RecordTags))]
    public ProvidedProperty? PropertyToProvide { get; set; }
}
