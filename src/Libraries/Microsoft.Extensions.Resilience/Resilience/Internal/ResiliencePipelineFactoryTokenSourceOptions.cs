// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Resilience.Internal;
internal sealed class ResiliencePipelineFactoryTokenSourceOptions<TResult>
{
    [Required]
    public List<Func<IChangeToken>> ChangeTokenSources { get; } = new();
}
