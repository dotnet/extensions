// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Http.Logging.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Logging.Test;

public class MediaTypeCollectionExtensionsTest
{
    private readonly string[] _readableContentTypes =
    {
        "application/*+json",
        "application/*+xml",
        "application/json",
        "application/xml",
        "text/*"
    };

    [Fact]
    public void Covers_WhenCovers_ReturnsTrue()
    {
        var collection = new HashSet<string>(_readableContentTypes, StringComparer.OrdinalIgnoreCase).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
        collection.Covers("application/xml").Should().BeTrue();
        collection.Covers("APPLICATION/XML").Should().BeTrue();
        collection.Covers("application/json").Should().BeTrue();
        collection.Covers("APPLICATION/JSON").Should().BeTrue();
        collection.Covers("application/atom+xml").Should().BeTrue();
        collection.Covers("APPLICATION/ATOM+XML").Should().BeTrue();
        collection.Covers("application/mud+json").Should().BeTrue();
        collection.Covers("APPLICATION/MUD+JSON").Should().BeTrue();
        collection.Covers("TEXT/WHATEVER").Should().BeTrue();
        collection.Covers("text/whatever").Should().BeTrue();
        collection.Covers("text/whatever-else").Should().BeTrue();
    }

    [Fact]
    public void Covers_WhenNotCovers_ReturnsFalse()
    {
        var collection = new HashSet<string>(_readableContentTypes, StringComparer.OrdinalIgnoreCase).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        collection.Covers(null!).Should().BeFalse();
        collection.Covers("").Should().BeFalse();
        collection.Covers("image").Should().BeFalse();
        collection.Covers("image/png").Should().BeFalse();
        collection.Covers("audio/ogg").Should().BeFalse();
        collection.Covers("application").Should().BeFalse();
        collection.Covers("application/octet-stream").Should().BeFalse();
        collection.Covers("application/x-httpd-php").Should().BeFalse();
        collection.Covers("application/json-seq").Should().BeFalse();
        collection.Covers("application/missing-blocks+cbor-seq").Should().BeFalse();
        collection.Covers("application/secevent+jwt").Should().BeFalse();
    }
}
