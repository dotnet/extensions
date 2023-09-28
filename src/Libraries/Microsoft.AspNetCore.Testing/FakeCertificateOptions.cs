// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Testing;

internal sealed class FakeCertificateOptions
{
    public X509Certificate2? Certificate { get; set; }
}
