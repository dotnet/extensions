// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Options.Validation.Test;

public class ValidateEnumeratedItemsAttributeTests
{
    [Fact]
    public void Basic()
    {
        var a = new ValidateEnumeratedItemsAttribute();
        Assert.NotNull(a);
        Assert.Null(a.Validator);

        a = new ValidateEnumeratedItemsAttribute(typeof(int));
        Assert.Equal(typeof(int), a.Validator);
    }
}
