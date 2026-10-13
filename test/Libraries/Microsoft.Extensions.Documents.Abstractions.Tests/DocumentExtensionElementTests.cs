// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Documents;
using Xunit;

namespace Microsoft.Extensions.Documents.Tests;

public class DocumentExtensionElementTests
{
    [Fact]
    public void PreservesOpaqueTypeAndValue()
    {
        object value = new();
        DocumentExtensionElement element = new("provider-extension", "stored extension", value);

        Assert.Equal("provider-extension", element.Type);
        Assert.Equal("stored extension", element.GetMarkdown());
        Assert.Same(value, element.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void RejectsEmptyType(string? type)
    {
        Assert.Throws<ArgumentNullException>(
            nameof(type),
            () => new DocumentExtensionElement(type!, "stored extension"));
    }
}
