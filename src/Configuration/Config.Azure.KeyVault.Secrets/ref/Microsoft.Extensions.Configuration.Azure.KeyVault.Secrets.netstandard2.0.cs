// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class AzureKeyVaultConfigurationExtensions
    {
        public static global::Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this global::Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, global::Azure.Security.KeyVault.Secrets.SecretClient client, global::Microsoft.Extensions.Configuration.Azure.KeyVault.Secrets.IKeyVaultSecretManager manager) { throw null; }
        public static global::Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this global::Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, global::Microsoft.Extensions.Configuration.Azure.KeyVault.Secrets.AzureKeyVaultConfigurationOptions options) { throw null; }
        public static global::Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this global::Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, global::System.Uri vault) { throw null; }
        public static global::Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this global::Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, global::System.Uri vault, global::Azure.Core.TokenCredential credential) { throw null; }
        public static global::Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this global::Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, global::System.Uri vault, global::Azure.Core.TokenCredential credential, global::Microsoft.Extensions.Configuration.Azure.KeyVault.Secrets.IKeyVaultSecretManager manager) { throw null; }
        public static global::Microsoft.Extensions.Configuration.IConfigurationBuilder AddAzureKeyVault(this global::Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, global::System.Uri vault, global::Microsoft.Extensions.Configuration.Azure.KeyVault.Secrets.IKeyVaultSecretManager manager) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.Azure.KeyVault.Secrets
{
    public partial class AzureKeyVaultConfigurationOptions
    {
        public AzureKeyVaultConfigurationOptions() { }
        public AzureKeyVaultConfigurationOptions(global::System.Uri vault, global::Azure.Core.TokenCredential credential) { }
        public global::Azure.Security.KeyVault.Secrets.SecretClient Client { [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public global::Microsoft.Extensions.Configuration.Azure.KeyVault.Secrets.IKeyVaultSecretManager Manager { [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public global::System.TimeSpan? ReloadInterval { [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class DefaultKeyVaultSecretManager : global::Microsoft.Extensions.Configuration.Azure.KeyVault.Secrets.IKeyVaultSecretManager
    {
        public DefaultKeyVaultSecretManager() { }
        public virtual string GetKey(global::Azure.Security.KeyVault.Secrets.KeyVaultSecret secret) { throw null; }
        public virtual bool Load(global::Azure.Security.KeyVault.Secrets.SecretProperties secret) { throw null; }
    }
    public partial interface IKeyVaultSecretManager
    {
        string GetKey(global::Azure.Security.KeyVault.Secrets.KeyVaultSecret secret);
        bool Load(global::Azure.Security.KeyVault.Secrets.SecretProperties secret);
    }
}
