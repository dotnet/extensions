// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Encapsulates options for <see cref="IResiliencePipelineFactory"/>.
/// </summary>
/// <typeparam name="TResult">The type of the result of action executed in pipeline.</typeparam>
/// <remarks>Scope of an instance of this class is limited per pipeline and configured using pipeline name.</remarks>
internal sealed class ResiliencePipelineFactoryOptions<TResult>
{
    /// <summary>
    /// Gets the actions used to configure <see cref="IPolicyPipelineBuilder{TResult}"/>.
    /// </summary>
    [Required]
    [Microsoft.Shared.Data.Validation.Length(1, ErrorMessage = "This resilience pipeline is not configured. Each resilience pipeline must include at least one policy. Field path: {0}")]
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
    public List<Action<IPolicyPipelineBuilder<TResult>>> BuilderActions { get; } = new();
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
}
