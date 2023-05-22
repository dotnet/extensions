// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Testing.Internal;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.AspNetCore.Testing.Test.Internal;

public class FakeCertificateFactoryTest
{
    [Fact]
    public void Create_CreatesCertificate()
    {
        using var certificate = FakeSslCertificateFactory.CreateSslCertificate();

        Assert.Equal("CN=r9-self-signed-unit-test-certificate", certificate.SubjectName.Name);
        Assert.Equal("localhost", certificate.GetNameInfo(X509NameType.DnsFromAlternativeName, false));
        Assert.True(DateTime.Now > certificate.NotBefore + TimeSpan.FromHours(1));
        Assert.True(DateTime.Now < certificate.NotAfter - TimeSpan.FromHours(1));
        Assert.False(certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single().Critical);
    }

    [ConditionalTheory]
    [OSSkipCondition(OperatingSystems.Linux)]
    [InlineData(false)]
    [InlineData(true)]
    public void GenerateRsa_RunsOnWindows_GeneratesRsa(bool runsOnWindows)
    {
        Assert.NotNull(FakeSslCertificateFactory.GenerateRsa(runsOnWindows));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows)]
    public void GenerateRsa_DoesNotRunOnWindows_GeneratesRsa()
    {
        Assert.NotNull(FakeSslCertificateFactory.GenerateRsa(runsOnWindows: false));
    }
}
