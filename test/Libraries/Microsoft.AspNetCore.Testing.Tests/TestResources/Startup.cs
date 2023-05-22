// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Testing.Test.TestResources;

[SuppressMessage("Performance", "CA1822", Justification = "convention")]
public class Startup
{
#if !NET6_0_OR_GREATER
    [SuppressMessage("Minor Code Smell", "S3257:Declarations and initializations should be as concise as possible", Justification = "Type parameters needed for proper resolution")]
#endif
    public void Configure(IApplicationBuilder app) => app.Use((HttpContext _, Func<Task> _) => Task.CompletedTask);

    public void ConfigureServices(IServiceCollection _)
    {
        // only need the class for testing
    }
}
