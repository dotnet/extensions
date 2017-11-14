// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Certificates.Generation
{
    internal class CertificateManager
    {
        public const string AspNetHttpsOid = "1.3.6.1.4.1.311.84.1.1";
        public const string AspNetHttpsOidFriendlyName = "ASP.NET Core HTTPS development certificate";

        public const string AspNetIdentityOid = "1.3.6.1.4.1.311.84.1.2";
        public const string AspNetIdentityOidFriendlyName = "ASP.NET Core Identity Json Web Token signing development certificate";

        private const string ServerAuthenticationEnhancedKeyUsageOid = "1.3.6.1.5.5.7.3.1";
        private const string ServerAuthenticationEnhancedKeyUsageOidFriendlyName = "Server Authentication";

        private const string LocalhostHttpsDnsName = "localhost";
        private const string LocalhostHttpsDistinguishedName = "CN=" + LocalhostHttpsDnsName;

        private const string IdentityDistinguishedName = "CN=Microsoft.AspNetCore.Identity.Signing";

        public const int RSAMinimumKeySizeInBits = 2048;

        private static readonly TimeSpan MaxRegexTimeout = TimeSpan.FromMinutes(1);
        private const string MacOSFindCertificateCommandLine = "security";
        private const string MacOSFindCertificateCommandLineArguments = "find-certificate -c localhost -a -Z -p /Library/Keychains/System.keychain";
        private const string MacOSFindCertificateOutputRegex = "SHA-1 hash: ([0-9A-Z]+)";
        private const string MacOSTrustCertificateCommandLine = "sudo";
        private const string MacOSTrustCertificateCommandLineArguments = "security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ";

        private const int UserCancelledErrorCode = 1223;

        public IList<X509Certificate2> ListCertificates(
            CertificatePurpose purpose,
            StoreName storeName,
            StoreLocation location,
            bool isValid,
            bool requireExportable = true)
        {
            var certificates = new List<X509Certificate2>();
            try
            {
                using (var store = new X509Store(storeName, location))
                {
                    store.Open(OpenFlags.ReadOnly);
                    certificates.AddRange(store.Certificates.OfType<X509Certificate2>());
                    IEnumerable<X509Certificate2> matchingCertificates = certificates;
                    switch (purpose)
                    {
                        case CertificatePurpose.All:
                            matchingCertificates = matchingCertificates
                                .Where(c => HasOid(c, AspNetHttpsOid) || HasOid(c, AspNetIdentityOid));
                            break;
                        case CertificatePurpose.HTTPS:
                            matchingCertificates = matchingCertificates
                                .Where(c => HasOid(c, AspNetHttpsOid));
                            break;
                        case CertificatePurpose.Signing:
                            matchingCertificates = matchingCertificates
                                .Where(c => HasOid(c, AspNetIdentityOid));
                            break;
                        default:
                            break;
                    }
                    if (isValid)
                    {
                        // Ensure the certificate hasn't expired, has a private key and its exportable
                        // (for container/unix scenarios).
                        var now = DateTimeOffset.Now;
                        matchingCertificates = matchingCertificates
                            .Where(c => c.NotBefore <= now &&
                                now <= c.NotAfter &&
                                (!requireExportable || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || IsExportable(c)));
                    }

                    // We need to enumerate the certificates early to prevent dispoisng issues.
                    matchingCertificates = matchingCertificates.ToList();

                    var certificatesToDispose = certificates.Except(matchingCertificates);
                    DisposeCertificates(certificatesToDispose);

                    store.Close();

                    return (IList<X509Certificate2>)matchingCertificates;
                }
            }
            catch
            {
                DisposeCertificates(certificates);
                certificates.Clear();
                return certificates;
            }

            bool HasOid(X509Certificate2 certificate, string oid) =>
                certificate.Extensions.OfType<X509Extension>()
                    .Any(e => string.Equals(oid, e.Oid.Value, StringComparison.Ordinal));

            bool IsExportable(X509Certificate2 c) =>
                ((c.GetRSAPrivateKey() is RSACryptoServiceProvider rsaPrivateKey &&
                    rsaPrivateKey.CspKeyContainerInfo.Exportable) ||
                (c.GetRSAPrivateKey() is RSACng cngPrivateKey &&
                    cngPrivateKey.Key.ExportPolicy == CngExportPolicies.AllowExport));
        }

        private void DisposeCertificates(IEnumerable<X509Certificate2> disposables)
        {
            foreach (var disposable in disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                }
            }
        }

#if NETCOREAPP2_0 || NETCOREAPP2_1

        public X509Certificate2 CreateAspNetCoreHttpsDevelopmentCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter, string subjectOverride)
        {
            var subject = new X500DistinguishedName(subjectOverride ?? LocalhostHttpsDistinguishedName);
            var extensions = new List<X509Extension>();
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName(LocalhostHttpsDnsName);

            var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.KeyEncipherment, critical: true);
            var enhancedKeyUsage = new X509EnhancedKeyUsageExtension(
                new OidCollection() {
                    new Oid(
                        ServerAuthenticationEnhancedKeyUsageOid,
                        ServerAuthenticationEnhancedKeyUsageOidFriendlyName)
                },
                critical: true);

            var basicConstraints = new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true);

            var aspNetHttpsExtension = new X509Extension(
                new AsnEncodedData(
                    new Oid(AspNetHttpsOid, AspNetHttpsOidFriendlyName),
                    Encoding.ASCII.GetBytes(AspNetHttpsOidFriendlyName)),
                critical: false);

            extensions.Add(basicConstraints);
            extensions.Add(keyUsage);
            extensions.Add(enhancedKeyUsage);
            extensions.Add(sanBuilder.Build(critical: true));
            extensions.Add(aspNetHttpsExtension);

            var certificate = CreateSelfSignedCertificate(subject, extensions, notBefore, notAfter);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                certificate.FriendlyName = AspNetHttpsOidFriendlyName;
            }

            return certificate;
        }

        public X509Certificate2 CreateApplicationTokenSigningDevelopmentCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter, string subjectOverride)
        {
            var subject = new X500DistinguishedName(subjectOverride ?? IdentityDistinguishedName);
            var extensions = new List<X509Extension>();

            var keyUsage = new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true);
            var enhancedKeyUsage = new X509EnhancedKeyUsageExtension(
                new OidCollection() {
                    new Oid(
                        ServerAuthenticationEnhancedKeyUsageOid,
                        ServerAuthenticationEnhancedKeyUsageOidFriendlyName)
                },
                critical: true);

            var basicConstraints = new X509BasicConstraintsExtension(
                certificateAuthority: false,
                hasPathLengthConstraint: false,
                pathLengthConstraint: 0,
                critical: true);

            var aspNetIdentityExtension = new X509Extension(
                new AsnEncodedData(
                    new Oid(AspNetIdentityOid, AspNetIdentityOidFriendlyName),
                    Encoding.ASCII.GetBytes(AspNetIdentityOidFriendlyName)),
                critical: false);

            extensions.Add(basicConstraints);
            extensions.Add(keyUsage);
            extensions.Add(enhancedKeyUsage);
            extensions.Add(aspNetIdentityExtension);

            var certificate = CreateSelfSignedCertificate(subject, extensions, notBefore, notAfter);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                certificate.FriendlyName = AspNetIdentityOidFriendlyName;
            }

            return certificate;
        }

        public X509Certificate2 CreateSelfSignedCertificate(
            X500DistinguishedName subject,
            IEnumerable<X509Extension> extensions,
            DateTimeOffset notBefore,
            DateTimeOffset notAfter)
        {
            var key = CreateKeyMaterial(RSAMinimumKeySizeInBits);

            var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            foreach (var extension in extensions)
            {
                request.CertificateExtensions.Add(extension);
            }

            return request.CreateSelfSigned(notBefore, notAfter);

            RSA CreateKeyMaterial(int minimumKeySize)
            {
                var rsa = RSA.Create(minimumKeySize);
                if (rsa.KeySize < minimumKeySize)
                {
                    throw new InvalidOperationException($"Failed to create a key with a size of {minimumKeySize} bits");
                }

                return rsa;
            }
        }

        public X509Certificate2 SaveCertificateInStore(X509Certificate2 certificate, StoreName name, StoreLocation location)
        {
            var imported = certificate;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // On non OSX systems we need to export the certificate and import it so that the transient
                // key that we generated gets persisted.
                var export = certificate.Export(X509ContentType.Pkcs12, "");
                imported = new X509Certificate2(export, "", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
                Array.Clear(export, 0, export.Length);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                imported.FriendlyName = certificate.FriendlyName;
            }

            using (var store = new X509Store(name, location))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(imported);
                store.Close();
            };

            return imported;
        }

        public void ExportCertificate(X509Certificate2 certificate, string path, bool includePrivateKey, string password)
        {
            if (Path.GetDirectoryName(path) != "")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            if (includePrivateKey)
            {
                var bytes = certificate.Export(X509ContentType.Pkcs12, password);
                try
                {
                    File.WriteAllBytes(path, bytes);
                }
                finally
                {
                    Array.Clear(bytes, 0, bytes.Length);
                }
            }
            else
            {
                var bytes = certificate.Export(X509ContentType.Cert);
                File.WriteAllBytes(path, bytes);
            }
        }

        public void TrustCertificate(X509Certificate2 certificate)
        {
            // Strip certificate of the private key if any.
            var publicCertificate = new X509Certificate2(certificate.Export(X509ContentType.Cert));

            if (!IsTrusted(publicCertificate))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    TrustCertificateOnWindows(certificate, publicCertificate);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    TrustCertificateOnMac(publicCertificate);
                }
            }
        }

        private void TrustCertificateOnMac(X509Certificate2 publicCertificate)
        {
            var tmpFile = Path.GetTempFileName();
            try
            {
                ExportCertificate(publicCertificate, tmpFile, includePrivateKey: false, password: null);
                var process = Process.Start(MacOSTrustCertificateCommandLine, MacOSTrustCertificateCommandLineArguments + tmpFile);
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException("There was an error trusting the certificate.");
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tmpFile))
                    {
                        File.Delete(tmpFile);
                    }
                }
                catch
                {
                    // We don't care if we can't delete the temp file.
                }
            }
        }

        private static void TrustCertificateOnWindows(X509Certificate2 certificate, X509Certificate2 publicCertificate)
        {
            publicCertificate.FriendlyName = certificate.FriendlyName;

            using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    store.Add(publicCertificate);
                }
                catch (CryptographicException exception) when (exception.HResult == UserCancelledErrorCode)
                {
                    throw new UserCancelledTrustException();
                }
                store.Close();
            };
        }

        public bool IsTrusted(X509Certificate2 certificate)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ListCertificates(CertificatePurpose.HTTPS, StoreName.Root, StoreLocation.CurrentUser, isValid: true, requireExportable: false)
                    .Any(c => c.Thumbprint == certificate.Thumbprint);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var checkTrustProcess = Process.Start(new ProcessStartInfo(MacOSFindCertificateCommandLine, MacOSFindCertificateCommandLineArguments)
                {
                    RedirectStandardOutput = true
                });

                checkTrustProcess.WaitForExit();
                var output = checkTrustProcess.StandardOutput.ReadToEnd();
                var matches = Regex.Matches(output, MacOSFindCertificateOutputRegex, RegexOptions.Multiline, MaxRegexTimeout);
                var hashes = matches.OfType<Match>().Select(m => m.Groups[1].Value).ToList();
                return !hashes.Any(h => string.Equals(h, certificate.Thumbprint, StringComparison.Ordinal));
            }
            else
            {
                return false;
            }
        }

        public void RemoveAllCertificates(CertificatePurpose purpose, StoreName storeName, StoreLocation storeLocation, string subject = null)
        {
            var certificates = ListCertificates(purpose, storeName, storeLocation, isValid: false);
            var certificatesWithName = subject == null ? certificates : certificates.Where(c => c.Subject == subject);

            RemoveAllCertificatesCore(certificatesWithName, storeName, storeLocation);
            DisposeCertificates(certificates);
        }

        public void RemoveAllCertificatesCore(IEnumerable<X509Certificate2> certificates, StoreName storeName, StoreLocation storeLocation)
        {
            using (var store = new X509Store(storeName, storeLocation))
            {
                store.Open(OpenFlags.ReadWrite);
                foreach (var certificate in certificates)
                {
                    var matching = store.Certificates.OfType<X509Certificate2>().Single(c => c.SerialNumber == certificate.SerialNumber);
                    store.Remove(matching);
                }
                store.Close();
            }
        }

        public EnsureCertificateResult EnsureAspNetCoreHttpsDevelopmentCertificate(
            DateTimeOffset notBefore,
            DateTimeOffset notAfter,
            string path = null,
            bool trust = false,
            bool includePrivateKey = false,
            string password = null,
            string subject = LocalhostHttpsDistinguishedName)
        {
            return EnsureValidCertificateExists(notBefore, notAfter, CertificatePurpose.HTTPS, path, trust, includePrivateKey, password, subject);
        }

        public EnsureCertificateResult EnsureAspNetCoreApplicationTokensDevelopmentCertificate(
            DateTimeOffset notBefore,
            DateTimeOffset notAfter,
            string path = null,
            bool trust = false,
            bool includePrivateKey = false,
            string password = null,
            string subject = IdentityDistinguishedName)
        {
            return EnsureValidCertificateExists(notBefore, notAfter, CertificatePurpose.Signing, path, trust, includePrivateKey, password, subject);
        }

        public EnsureCertificateResult EnsureValidCertificateExists(
            DateTimeOffset notBefore,
            DateTimeOffset notAfter,
            CertificatePurpose purpose,
            string path = null,
            bool trust = false,
            bool includePrivateKey = false,
            string password = null,
            string subjectOverride = null)
        {
            if (purpose == CertificatePurpose.All)
            {
                throw new ArgumentException("The certificate must have a specific purpose.");
            }

            var certificates = ListCertificates(purpose, StoreName.My, StoreLocation.CurrentUser, isValid: true).Concat(
                ListCertificates(purpose, StoreName.My, StoreLocation.LocalMachine, isValid: true));

            certificates = subjectOverride == null ? certificates : certificates.Where(c => c.Subject == subjectOverride);

            var result = EnsureCertificateResult.Succeeded;

            X509Certificate2 certificate = null;
            if (certificates.Count() > 0)
            {
                certificate = certificates.FirstOrDefault();
                result = EnsureCertificateResult.ValidCertificatePresent;
            }
            else
            {
                try
                {
                    switch (purpose)
                    {
                        case CertificatePurpose.All:
                            throw new InvalidOperationException("The certificate must have a specific purpose.");
                        case CertificatePurpose.HTTPS:
                            certificate = CreateAspNetCoreHttpsDevelopmentCertificate(notBefore, notAfter, subjectOverride);
                            break;
                        case CertificatePurpose.Signing:
                            certificate = CreateApplicationTokenSigningDevelopmentCertificate(notBefore, notAfter, subjectOverride);
                            break;
                        default:
                            throw new InvalidOperationException("The certificate must have a purpose.");
                    }
                }
                catch
                {
                    return EnsureCertificateResult.ErrorCreatingTheCertificate;
                }

                try
                {
                    certificate = SaveCertificateInStore(certificate, StoreName.My, StoreLocation.CurrentUser);
                }
                catch
                {
                    return EnsureCertificateResult.ErrorSavingTheCertificateIntoTheCurrentUserPersonalStore;
                }
            }
            if (path != null)
            {
                try
                {
                    ExportCertificate(certificate, path, includePrivateKey, password);
                }
                catch
                {
                    return EnsureCertificateResult.ErrorExportingTheCertificate;
                }
            }

            if ((RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) && trust)
            {
                try
                {
                    TrustCertificate(certificate);
                }
                catch(UserCancelledTrustException)
                {
                    return EnsureCertificateResult.UserCancelledTrustStep;
                }
                catch
                {
                    return EnsureCertificateResult.FailedToTrustTheCertificate;
                }
            }

            return result;
        }

        private class UserCancelledTrustException : Exception
        {
        }
#endif
    }
}