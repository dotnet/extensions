// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using System.Threading;

namespace Microsoft.Extensions.Configuration.AzureKeyVault
{
    /// <summary>
    /// An AzureKeyVault based <see cref="ConfigurationProvider"/>.
    /// </summary>
    internal class AzureKeyVaultConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly TimeSpan? _reloadPollDelay;
        private readonly IKeyVaultClient _client;
        private readonly string _vault;
        private readonly IKeyVaultSecretManager _manager;
        private Dictionary<string, SecretAttributes> _loadedSecrets;
        private Task _pollingTask;
        private CancellationTokenSource _cancellationToken;

        /// <summary>
        /// Creates a new instance of <see cref="AzureKeyVaultConfigurationProvider"/>.
        /// </summary>
        /// <param name="client">The <see cref="KeyVaultClient"/> to use for retrieving values.</param>
        /// <param name="vault">Azure KeyVault uri.</param>
        /// <param name="manager"></param>
        /// <param name="reloadPollDelay">The timespan to wait inbetween each attempt at polling the Azure KeyVault for changes. Default is null which indicates no reloading.</param>
        public AzureKeyVaultConfigurationProvider(IKeyVaultClient client, string vault, IKeyVaultSecretManager manager, TimeSpan? reloadPollDelay = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _vault = vault ?? throw new ArgumentNullException(nameof(vault));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            if (_reloadPollDelay != null && _reloadPollDelay.Value == TimeSpan.Zero)
            {
                throw new ArgumentException(nameof(reloadPollDelay));
            }

            _pollingTask = null;
            _cancellationToken = new CancellationTokenSource();
            _reloadPollDelay = reloadPollDelay;
            _loadedSecrets = new Dictionary<string, SecretAttributes>();
        }

        public override void Load() => LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        private async Task PollForSecretChangesAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(_reloadPollDelay.Value.Milliseconds);
                Load();
            }
        }

        private bool isSecretAddedOrUpdated(SecretItem secretItem)
        {
            bool result = false;

            if (_manager.Load(secretItem) && secretItem.Attributes?.Enabled == true)
            {
                string key = secretItem.Identifier.Name;
                bool isKeyLoaded = _loadedSecrets.ContainsKey(key);

                //a new key has been added
                if(!isKeyLoaded)
                {
                    return true;
                }

                var secretUpdateTime = secretItem.Attributes.Updated;
                var cachedUpdateTime = _loadedSecrets[key]?.Updated;
                bool isUpdateTimeknown = (secretUpdateTime != null) && (cachedUpdateTime != null);
                bool isKeyUnchanged = isUpdateTimeknown &&
                                        (cachedUpdateTime?.Ticks == secretUpdateTime?.Ticks);
                result = !isKeyUnchanged;
            }
            return result;
        }

        private async Task LoadAsync()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var secrets = await _client.GetSecretsAsync(_vault).ConfigureAwait(false);
            var secretList = new List<SecretItem>(secrets.Count());
            var tasks = new List<Task<SecretBundle>>(secrets.Count());
            bool isReloadNeeded = false;

            // fetch all the secretItems
            do
            {
                secretList.AddRange(secrets.ToList());
                secrets = secrets.NextPageLink != null ?
                    await _client.GetSecretsNextAsync(secrets.NextPageLink).ConfigureAwait(false) :
                     null;
            } while (secrets != null);

            // check if any secrets have been removed
            if(secretList.Count() != _loadedSecrets.Count)
            {
                isReloadNeeded = true;
            }

            // check if any secrets have been added or updated
            if (!isReloadNeeded)
            {
                foreach (var secretItem in secretList)
                {
                    if (isSecretAddedOrUpdated(secretItem))
                    {
                        isReloadNeeded = true;
                        break;
                    }
                }
            }

            // reload secret values if needed
            if (isReloadNeeded)
            {
                tasks.Clear();
                _loadedSecrets.Clear();

                foreach (var secretItem in secretList)
                {
                    if (_manager.Load(secretItem) && secretItem.Attributes?.Enabled == true)
                    {
                        tasks.Add(_client.GetSecretAsync(secretItem.Id));
                    }
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var task in tasks)
                {
                    data.Add(_manager.GetKey(task.Result), task.Result.Value);
                    _loadedSecrets.Add(task.Result.SecretIdentifier.Name, task.Result.Attributes);
                }

                Data = data;
                OnReload();
            } 

            // schedule a polling task only if none exists and a valid delay is specified
            if (_pollingTask == null && _reloadPollDelay != null)
            {
                _pollingTask = PollForSecretChangesAsync(_cancellationToken.Token);
            }
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
        }
    }
}
