// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.AI;

internal sealed class OllamaChatRequestMessage
{
    public required string Role { get; set; }
    public string? Content { get; set; }
    public IList<string>? Images { get; set; }
}
