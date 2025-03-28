// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Diagnostics.Buffering;

[OptionsValidator]
internal sealed partial class PerRequestLogBufferingOptionsValidator : IValidateOptions<PerRequestLogBufferingOptions>
{
}
#endif
