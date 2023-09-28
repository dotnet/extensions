// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization.Test;

public class ExceptionSummaryExtensionsTests
{
    [Fact]
    public void AddExceptionSummarizer_WithServiceCollection_AddsExceptionSummarizer()
    {
        IServiceCollection services = new ServiceCollection();
        services = services.AddExceptionSummarizer();

        var summarizer = services.BuildServiceProvider().GetService<IExceptionSummarizer>();

        Assert.NotNull(summarizer);
        Assert.IsType<ExceptionSummarizer>(summarizer);
    }
}
