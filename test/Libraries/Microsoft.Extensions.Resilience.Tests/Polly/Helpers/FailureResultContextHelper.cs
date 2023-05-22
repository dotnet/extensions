// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;

namespace Microsoft.Extensions.Resilience.Polly.Test.Helpers;

public static class FailureResultContextHelper
{
    public static Func<T, FailureResultContext> GetFailureResultContextProvider<T>(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<FailureEventMetricsOptions<T>>>();

        return value => options.Value.GetContextFromResult(value);
    }
}
