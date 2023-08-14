// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.AutoClient;

/// <summary>
/// Validator for <see cref="AutoClientOptions"/>.
/// </summary>
/// <remarks>
/// This type is not intended to be directly invoked by application code.
/// It's intended to be invoked by generated code.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[OptionsValidator]
public sealed partial class AutoClientOptionsValidator : IValidateOptions<AutoClientOptions>
{
}
