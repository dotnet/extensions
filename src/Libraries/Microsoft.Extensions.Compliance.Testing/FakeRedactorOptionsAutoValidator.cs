// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Compliance.Testing;

[OptionsValidator]
internal sealed partial class FakeRedactorOptionsAutoValidator : IValidateOptions<FakeRedactorOptions>
{
}
