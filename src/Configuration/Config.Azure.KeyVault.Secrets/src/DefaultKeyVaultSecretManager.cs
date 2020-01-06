// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Azure.Security.KeyVault.Secrets;

namespace Microsoft.Extensions.Configuration.KeyVault.Secrets
{
    /// <summary>
    /// Default implementation of <see cref="IKeyVaultSecretManager"/> that loads all secrets
    /// and replaces '--' with ':" in key names.
    /// </summary>
    public class DefaultKeyVaultSecretManager : IKeyVaultSecretManager
    {
        internal static IKeyVaultSecretManager Instance { get; } = new DefaultKeyVaultSecretManager();

        /// <inheritdoc />
        public virtual string GetKey(KeyVaultSecret secret)
        {
            return secret.Name.Replace("--", ConfigurationPath.KeyDelimiter);
        }

        /// <inheritdoc />
        public virtual bool Load(SecretProperties secret)
        {
            return true;
        }
    }
}
