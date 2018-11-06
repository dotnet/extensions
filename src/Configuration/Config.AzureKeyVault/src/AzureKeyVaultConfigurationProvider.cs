// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// An AzureKeyVault based <see cref="ConfigurationProvider"/>.
    /// </summary>
    internal class AzureKeyVaultConfigurationProvider : ConfigurationProvider
    {
        private readonly IKeyVaultClient _client;
        private readonly string _vault;
        private readonly IKeyVaultSecretManager _manager;

        /// <summary>
        /// Creates a new instance of <see cref="AzureKeyVaultConfigurationProvider"/>.
        /// </summary>
        /// <param name="client">The <see cref="KeyVaultClient"/> to use for retrieving values.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="manager"></param>
        public AzureKeyVaultConfigurationProvider(IKeyVaultClient client, string vault, IKeyVaultSecretManager manager)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }
            if (vault == null)
            {
                throw new ArgumentNullException(nameof(vault));
            }
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            _client = client;
            _vault = vault;
            _manager = manager;
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task LoadAsync()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var secrets = await _client.GetSecretsAsync(_vault).ConfigureAwait(false);
            do
            {
                foreach (var secretItem in secrets)
                {
                    if (!_manager.Load(secretItem) || (secretItem.Attributes?.Enabled != true))
                    {
                        continue;
                    }

                    var value = await _client.GetSecretAsync(secretItem.Id).ConfigureAwait(false);
                    var key = _manager.GetKey(value);
                    data.Add(key, value.Value);
                }

                secrets = secrets.NextPageLink != null ?
                    await _client.GetSecretsNextAsync(secrets.NextPageLink).ConfigureAwait(false) :
                    null;
            } while (secrets != null);

            Data = data;
        }
    }
}