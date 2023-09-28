// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Test;

[SuppressMessage("Design", "CA1063", Justification = "not needed")]
public class FakeCertificateHttpClientHandlerTests : IDisposable
{
    private readonly X509Certificate2 _certificate = FakeSslCertificateFactory.CreateSslCertificate();
    private readonly X509Certificate2 _anotherCertificate = FakeSslCertificateFactory.CreateSslCertificate();
    private readonly HttpRequestMessage _request = new();

    [Fact]
    public void ServerCertificateCustomValidationCallback_OurCertProvided_ReturnsTrue()
    {
        using var sut = new FakeCertificateHttpClientHandler(_certificate);

        Assert.True(sut.ServerCertificateCustomValidationCallback!(
            _request,
            _certificate,
            null,
            SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [Fact]
    public void ServerCertificateCustomValidationCallback_DifferentCertAndNoErrors_ReturnsTrue()
    {
        using var sut = new FakeCertificateHttpClientHandler(_certificate);

        Assert.True(sut.ServerCertificateCustomValidationCallback!(
            _request,
            _anotherCertificate,
            null,
            SslPolicyErrors.None));
    }

    [Fact]
    public void ServerCertificateCustomValidationCallback_DifferentCertAndErrors_ReturnsFalse()
    {
        using var sut = new FakeCertificateHttpClientHandler(_certificate);

        Assert.False(sut.ServerCertificateCustomValidationCallback!(
            _request,
            _anotherCertificate,
            null,
            SslPolicyErrors.RemoteCertificateChainErrors));
    }

    [SuppressMessage("Usage", "CA1816", Justification = "not needed")]
    public void Dispose()
    {
        _certificate.Dispose();
        _anotherCertificate.Dispose();
        _request.Dispose();
    }
}
