// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization.Test;

public class HttpExceptionSummaryProviderExtensionsTests
{
    [Fact]
    public void AddHttpExceptionSummaryProvider_WithServiceCollection_AddsHttpExceptionSummaryProvider()
    {
        IServiceCollection services = new ServiceCollection();
        services = services.AddExceptionSummarizer(b => b.AddHttpProvider());

        var exceptionSummaryProvider = services.BuildServiceProvider().GetService<IExceptionSummaryProvider>();

        Assert.NotNull(exceptionSummaryProvider);
        Assert.IsType<HttpExceptionSummaryProvider>(exceptionSummaryProvider);
    }
}
