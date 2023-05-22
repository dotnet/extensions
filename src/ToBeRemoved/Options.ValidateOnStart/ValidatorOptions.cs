// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Options.Validation;

internal sealed class ValidatorOptions
{
    /// <remarks>
    /// The key maps to the tuple with (a) type of TOptions in <see cref="IOptions{TOptions}"/> and (b) name of options.
    /// The value is a method that accesses the <see cref="IOptions{TOptions}.Value"/> property in order to force evaluation of
    /// the options type.
    /// Default value is an empty <see cref="Dictionary{T, V}"/>.
    /// </remarks>
    public Dictionary<(Type optionsType, string optionsName), Action> Validators { get; } = new();
}

#endif
