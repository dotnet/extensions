// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Testing.Internal;

internal sealed class FakeCertificateHttpClientHandler : HttpClientHandler
{
    public FakeCertificateHttpClientHandler(X509Certificate2 certificate)
    {
        ServerCertificateCustomValidationCallback = (_, serverCertificate, _, errors) =>
        {
            if (serverCertificate is null || !serverCertificate.Equals(certificate))
            {
                return errors == SslPolicyErrors.None;
            }

            return true;
        };
    }
}
