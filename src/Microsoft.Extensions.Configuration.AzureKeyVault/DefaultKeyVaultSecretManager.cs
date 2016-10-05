// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Azure.KeyVault;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// Default implementation of <see cref="IKeyVaultSecretManager"/> that loads all secrets
    /// and replaces '__' with ':" in key names.
    /// </summary>
    public class DefaultKeyVaultSecretManager : IKeyVaultSecretManager
    {
        /// <inheritdoc />
        public virtual string GetKey(Secret secret)
        {
            return secret.SecretIdentifier.Name.Replace("--", ConfigurationPath.KeyDelimiter);
        }

        /// <inheritdoc />
        public virtual bool Load(SecretItem secret)
        {
            return true;
        }
    }
}