// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// Default implementation of <see cref="IKeyVaultSecretManager"/> that loads all secrets
    /// and replaces '--' with ':" in key names.
    /// </summary>
    public class DefaultKeyVaultSecretManager : IKeyVaultSecretManager
    {
        internal static IKeyVaultSecretManager Instance { get; } = new DefaultKeyVaultSecretManager();

        /// <inheritdoc />
        public virtual string GetKey(SecretBundle secret)
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
