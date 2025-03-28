// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#if NET9_0_OR_GREATER

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Buffering;

[OptionsValidator]
internal sealed partial class GlobalLogBufferingOptionsValidator : IValidateOptions<GlobalLogBufferingOptions>
{
}
#endif
