// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Resilience;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The builder for configuring the HTTP client resilience pipeline.
/// </summary>
#pragma warning disable S4023 // Interfaces should not be empty
public interface IHttpResiliencePipelineBuilder : IResiliencePipelineBuilder<HttpResponseMessage>
#pragma warning restore S4023 // Interfaces should not be empty
{
}
