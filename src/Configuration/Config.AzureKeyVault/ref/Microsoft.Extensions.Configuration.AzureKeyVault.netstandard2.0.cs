// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class AzureKeyVaultConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, Microsoft.Extensions.Configuration.AzureKeyVault.AzureKeyVaultConfigurationOptions options) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string vault) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string vault, Microsoft.Azure.KeyVault.KeyVaultClient client, Microsoft.Extensions.Configuration.AzureKeyVault.IKeyVaultSecretManager manager) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string vault, Microsoft.Extensions.Configuration.AzureKeyVault.IKeyVaultSecretManager manager) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string vault, string clientId, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string vault, string clientId, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, Microsoft.Extensions.Configuration.AzureKeyVault.IKeyVaultSecretManager manager) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string vault, string clientId, string clientSecret) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string vault, string clientId, string clientSecret, Microsoft.Extensions.Configuration.AzureKeyVault.IKeyVaultSecretManager manager) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    public partial class AzureKeyVaultConfigurationOptions
    {
        public AzureKeyVaultConfigurationOptions() { }
        public AzureKeyVaultConfigurationOptions(string vault) { }
        public AzureKeyVaultConfigurationOptions(string vault, string clientId, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public AzureKeyVaultConfigurationOptions(string vault, string clientId, string clientSecret) { }
        public Microsoft.Azure.KeyVault.KeyVaultClient Client { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.Extensions.Configuration.AzureKeyVault.IKeyVaultSecretManager Manager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan? ReloadInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Vault { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class DefaultKeyVaultSecretManager : Microsoft.Extensions.Configuration.AzureKeyVault.IKeyVaultSecretManager
    {
        public DefaultKeyVaultSecretManager() { }
        public virtual string GetKey(Microsoft.Azure.KeyVault.Models.SecretBundle secret) { throw null; }
        public virtual bool Load(Microsoft.Azure.KeyVault.Models.SecretItem secret) { throw null; }
    }
    public partial interface IKeyVaultSecretManager
    {
        string GetKey(Microsoft.Azure.KeyVault.Models.SecretBundle secret);
        bool Load(Microsoft.Azure.KeyVault.Models.SecretItem secret);
    }
}
