// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test;

public class IncomingRequestStructTest
{
    [Fact]
    public void Should_ReturnEnumerator_WithElements()
    {
        var helper = LogMethodHelper.GetHelper();
        helper.Add("prop1", "value1");
        helper.Add("prop_2", "value_2");

        var reqStruct = new Log.IncomingRequestStruct(helper);
        var enumerable = (IEnumerable)reqStruct;
        var list = new List<object?>();
        foreach (var item in enumerable)
        {
            list.Add(item);
        }

        Assert.Collection(list,
            x => Assert.Equal(helper[0], x),
            x => Assert.Equal(helper[1], x));
    }
}
