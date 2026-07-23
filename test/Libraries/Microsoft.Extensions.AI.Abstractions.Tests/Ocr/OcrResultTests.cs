// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AI;

public class OcrResultTests
{
    [Fact]
    public void Constructor_NullPages_Throws()
    {
        Assert.Throws<ArgumentNullException>("pages", () => new OcrResult(null!));
    }

    [Fact]
    public void Text_JoinsPerPageText()
    {
        var result = new OcrResult([new OcrPage(1, "page one"), new OcrPage(2, "page two")]);

        Assert.Equal("page one\n\npage two", result.Text);
        Assert.Equal(2, result.Pages.Count);
    }
}
