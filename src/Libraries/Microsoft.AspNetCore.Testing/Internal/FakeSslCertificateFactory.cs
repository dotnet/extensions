// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Testing.Internal;

/// <remarks>Copied from Microsoft.Extensions.Secrets.Test.TestCertificateFactory.
/// Could be exposed in a new package called Secrets.Fakes to facilitate reusability.</remarks>
internal static class FakeSslCertificateFactory
{
    private static readonly RSA _rsa = GenerateRsa(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

    /// <summary>
    /// Creates a self-signed <see cref="X509Certificate2"/> instance for testing.
    /// </summary>
    /// <returns>An <see cref="X509Certificate2"/> instance for testing.</returns>
    [SuppressMessage("Reliability", "R9A022:Use System.TimeProvider when dealing with time in your code.", Justification = "declarations")]
    public static X509Certificate2 CreateSslCertificate()
    {
        var request = new CertificateRequest(
            new X500DistinguishedName("CN=r9-self-signed-unit-test-certificate"),
            _rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        request.CertificateExtensions.Add(sanBuilder.Build());

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new("1.3.6.1.5.5.7.3.1"), // serverAuth Object ID - indicates that the certificate is an SSL server certificate
                new("1.3.6.1.5.5.7.3.2") // clientAuth Object ID - indicates that the certificate is an SSL client certificate
            },
            false));

        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));
    }

    [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility",
        Justification = "Must use RSACryptoServiceProvider on windows, otherwise X509Certificate2.GetRSAPrivateKey().ExportPkcs8PrivateKey() will throw.")]
    internal static RSA GenerateRsa(bool runsOnWindows)
    {
        // Stryker disable all
        return runsOnWindows
            ? new RSACryptoServiceProvider(
                2048,
                new CspParameters(24, "Microsoft Enhanced RSA and AES Cryptographic Provider", Guid.NewGuid().ToString()))
            : RSA.Create();
    }
}
