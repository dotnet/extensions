// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileStoreContentTests
{
    [Fact]
    public void Constructor_InvalidInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new HostedFileStoreContent(null!));
        Assert.Throws<ArgumentException>(() => new HostedFileStoreContent(string.Empty));
        Assert.Throws<ArgumentException>(() => new HostedFileStoreContent(" "));
    }

    [Fact]
    public void Constructor_String_PropsDefault()
    {
        string fileId = "id123";
        HostedFileStoreContent c = new(fileId);
        Assert.Null(c.RawRepresentation);
        Assert.Null(c.AdditionalProperties);
        Assert.Equal(fileId, c.FileStoreId);
    }

    [Fact]
    public void Constructor_PropsRoundtrip()
    {
        HostedFileStoreContent c = new("id123");

        Assert.Equal("id123", c.FileStoreId);
        c.FileStoreId = "id456";
        Assert.Equal("id456", c.FileStoreId);

        Assert.Throws<ArgumentNullException>(() => c.FileStoreId = null!);
        Assert.Throws<ArgumentException>(() => c.FileStoreId = string.Empty);
        Assert.Throws<ArgumentException>(() => c.FileStoreId = " ");
        Assert.Equal("id456", c.FileStoreId);

        Assert.Null(c.RawRepresentation);
        object raw = new();
        c.RawRepresentation = raw;
        Assert.Same(raw, c.RawRepresentation);

        Assert.Null(c.AdditionalProperties);
        AdditionalPropertiesDictionary props = new() { { "key", "value" } };
        c.AdditionalProperties = props;
        Assert.Same(props, c.AdditionalProperties);
    }
}
