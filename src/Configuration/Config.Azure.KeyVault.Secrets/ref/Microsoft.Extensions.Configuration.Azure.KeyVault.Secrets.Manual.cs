// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class AzureKeyVaultConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, Azure.Security.KeyVault.Secrets.SecretClient client, Microsoft.Extensions.Configuration.KeyVault.Secrets.IKeyVaultSecretManager manager) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, System.Uri vault, Azure.Core.TokenCredential credential) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, System.Uri vault, Azure.Core.TokenCredential credential, Microsoft.Extensions.Configuration.KeyVault.Secrets.IKeyVaultSecretManager manager) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.KeyVault.Secrets
{
    public partial class AzureKeyVaultConfigurationOptions
    {
        public AzureKeyVaultConfigurationOptions(System.Uri vault, Azure.Core.TokenCredential credential) { }
        public Azure.Security.KeyVault.Secrets.SecretClient Client { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class DefaultKeyVaultSecretManager : Microsoft.Extensions.Configuration.KeyVault.Secrets.IKeyVaultSecretManager
    {
        public virtual string GetKey(Azure.Security.KeyVault.Secrets.KeyVaultSecret secret) { throw null; }
        public virtual bool Load(Azure.Security.KeyVault.Secrets.SecretProperties secret) { throw null; }
    }
    public partial interface IKeyVaultSecretManager
    {
        string GetKey(Azure.Security.KeyVault.Secrets.KeyVaultSecret secret);
        bool Load(Azure.Security.KeyVault.Secrets.SecretProperties secret);
    }
}