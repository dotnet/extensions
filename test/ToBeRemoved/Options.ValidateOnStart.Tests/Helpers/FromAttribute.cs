// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Options.Validation.Test;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class FromAttribute : ValidationAttribute
{
    public string? Accepted { get; set; }

    public override bool IsValid(object? value) => value?.ToString() == Accepted;
}
