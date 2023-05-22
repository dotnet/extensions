// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual;

/// <summary>
/// Generates an implementation of <see cref="IOptionsContext"/> for the annotated type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class OptionsContextAttribute : Attribute
{
}
