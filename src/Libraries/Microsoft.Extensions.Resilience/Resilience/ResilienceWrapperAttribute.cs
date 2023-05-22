// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// This attribute is used by the source generator
/// in order to generate code for a resilient cache.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ResilienceWrapperAttribute : Attribute
{
}
