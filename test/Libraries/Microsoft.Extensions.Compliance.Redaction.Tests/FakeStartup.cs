// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Compliance.Redaction.Test;

[SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "Instantiated via reflection.")]
internal class FakeStartup
{
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Because then I get another warning that when type contains just static stuff it can be all made static. When I will make type static I cannot use it as generic parameter.")]
    public void Configure()
    {
        // WebHostBuilder requirement
    }
}
