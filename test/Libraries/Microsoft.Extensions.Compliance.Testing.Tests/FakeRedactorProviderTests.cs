// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;
using Xunit;

namespace Microsoft.Extensions.Compliance.Testing.Tests;

public class FakeRedactorProviderTests
{
    [Fact]
    public void Basic()
    {
        var provider = new FakeRedactorProvider();

        var dc = new DataClassification("TAX", "1");
        var redactor = provider.GetRedactor(dc);
        Assert.Equal("Hello", redactor.Redact("Hello"));
    }

    [Fact]
    public void Can_Access_Event_Collector_From_Within_Redactor_Provider()
    {
        var rp = new FakeRedactorProvider();
        var dc = new DataClassification("TAX", "1");
        rp.GetRedactor(dc);
        Assert.Equal(dc, rp.Collector.LastRedactorRequested.DataClassifications);
    }
}
