// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable MEAI001

using Xunit;

namespace Microsoft.Extensions.AI;

public class HostedFileClientOptionsTests
{
    [Fact]
    public void UploadOptions_PropsDefault()
    {
        var options = new HostedFileUploadOptions();
        Assert.Null(options.Purpose);
        Assert.Null(options.Scope);
        Assert.Null(options.AdditionalProperties);
    }

    [Fact]
    public void UploadOptions_PropsRoundtrip()
    {
        var props = new AdditionalPropertiesDictionary { { "key", "value" } };
        var options = new HostedFileUploadOptions
        {
            Purpose = "fine-tune",
            Scope = "container-1",
            AdditionalProperties = props
        };

        Assert.Equal("fine-tune", options.Purpose);
        Assert.Equal("container-1", options.Scope);
        Assert.Same(props, options.AdditionalProperties);
    }

    [Fact]
    public void DownloadOptions_PropsDefault()
    {
        var options = new HostedFileDownloadOptions();
        Assert.Null(options.Scope);
        Assert.Null(options.AdditionalProperties);
    }

    [Fact]
    public void DownloadOptions_PropsRoundtrip()
    {
        var props = new AdditionalPropertiesDictionary { { "key", "value" } };
        var options = new HostedFileDownloadOptions
        {
            Scope = "scope-1",
            AdditionalProperties = props
        };

        Assert.Equal("scope-1", options.Scope);
        Assert.Same(props, options.AdditionalProperties);
    }

    [Fact]
    public void GetOptions_PropsDefault()
    {
        var options = new HostedFileGetOptions();
        Assert.Null(options.Scope);
        Assert.Null(options.AdditionalProperties);
    }

    [Fact]
    public void GetOptions_PropsRoundtrip()
    {
        var props = new AdditionalPropertiesDictionary { { "k", "v" } };
        var options = new HostedFileGetOptions
        {
            Scope = "scope-2",
            AdditionalProperties = props
        };

        Assert.Equal("scope-2", options.Scope);
        Assert.Same(props, options.AdditionalProperties);
    }

    [Fact]
    public void DeleteOptions_PropsDefault()
    {
        var options = new HostedFileDeleteOptions();
        Assert.Null(options.Scope);
        Assert.Null(options.AdditionalProperties);
    }

    [Fact]
    public void DeleteOptions_PropsRoundtrip()
    {
        var props = new AdditionalPropertiesDictionary { { "k", "v" } };
        var options = new HostedFileDeleteOptions
        {
            Scope = "scope-3",
            AdditionalProperties = props
        };

        Assert.Equal("scope-3", options.Scope);
        Assert.Same(props, options.AdditionalProperties);
    }

    [Fact]
    public void ListOptions_PropsDefault()
    {
        var options = new HostedFileListOptions();
        Assert.Null(options.Purpose);
        Assert.Null(options.Limit);
        Assert.Null(options.Scope);
        Assert.Null(options.AdditionalProperties);
    }

    [Fact]
    public void ListOptions_PropsRoundtrip()
    {
        var props = new AdditionalPropertiesDictionary { { "k", "v" } };
        var options = new HostedFileListOptions
        {
            Purpose = "assistants",
            Limit = 50,
            Scope = "scope-4",
            AdditionalProperties = props
        };

        Assert.Equal("assistants", options.Purpose);
        Assert.Equal(50, options.Limit);
        Assert.Equal("scope-4", options.Scope);
        Assert.Same(props, options.AdditionalProperties);
    }
}
