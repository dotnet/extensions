// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options.Contextual.Internal;

/// <summary>
/// Implementation of <see cref="IValidateContextualOptions{TOptions}"/>.
/// </summary>
/// <typeparam name="TOptions">The options type to validate.</typeparam>
internal sealed class ValidateContextualOptions<TOptions> : ValidateOptions<TOptions>, IValidateContextualOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateContextualOptions{TOptions}"/> class.
    /// </summary>
    /// <param name="name">Options name.</param>
    /// <param name="validation">Validation function.</param>
    /// <param name="failureMessage">Validation failure message.</param>
    public ValidateContextualOptions(string? name, Func<TOptions, bool> validation, string failureMessage)
        : base(name, validation, failureMessage)
    {
    }
}
