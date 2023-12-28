// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public class TagNameAttributeTests
{
    [Fact]
    public void Basic()
    {
        var a = new TagNameAttribute("a");
        Assert.Equal("a", a.Name);

        Assert.Throws<ArgumentNullException>(() => new TagNameAttribute(null!));
    }
}
