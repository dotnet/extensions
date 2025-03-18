// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

[OptionsValidator]
internal sealed partial class RandomProbabilisticSamplerOptionsValidator : IValidateOptions<RandomProbabilisticSamplerOptions>
{
}
