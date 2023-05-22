// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Options.Validation.Test;

public class ValidateObjectMembersAttributeTest
{
    [Fact]
    public void Basic()
    {
        var a = new ValidateObjectMembersAttribute();
        Assert.NotNull(a);
        Assert.Null(a.Validator);

        a = new ValidateObjectMembersAttribute(typeof(int));
        Assert.Equal(typeof(int), a.Validator);
    }
}
