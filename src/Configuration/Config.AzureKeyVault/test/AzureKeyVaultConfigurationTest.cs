// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Extensions.Configuration.Test;
using Microsoft.Extensions.Primitives;
using Microsoft.Rest.Azure;
using Moq;
using Xunit;
using Action = System.Action;

namespace Microsoft.Extensions.Configuration.AzureKeyVault.Test
{
    public class AzureKeyVaultConfigurationTest: ConfigurationProviderTestBase
    {
        private const string VaultUri = "https://vault";
        private static readonly TimeSpan NoReloadDelay = TimeSpan.FromMilliseconds(1);

        [Fact]
        public void LoadsAllSecretsFromVault()
        {
            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1")
                },
                new []
                {
                    CreateSecret("Secret2", "Value2")
                }
                );

            // Act
            using (var provider = new AzureKeyVaultConfigurationProvider(client, VaultUri, new DefaultKeyVaultSecretManager()))
            {
                provider.Load();

                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.Equal(new[] { "Secret1", "Secret2" }, childKeys);
                Assert.Equal("Value1", provider.Get("Secret1"));
                Assert.Equal("Value2", provider.Get("Secret2"));
            }
        }

        private (SecretAttributes attributes, SecretBundle bundle) CreateSecret(string name, string value, Func<SecretAttributes> attributesFactory = null, Action<SecretBundle> bundleAction = null)
        {
            var id = new SecretIdentifier(VaultUri, name).Identifier;
            var secretAttributes = attributesFactory?.Invoke() ?? new SecretAttributes() { Enabled = true };
            var secretBundle = new SecretBundle(VaultUri, id);
            secretBundle.Id = id;
            secretBundle.Attributes = secretAttributes;
            secretBundle.Value = value;
            bundleAction?.Invoke(secretBundle);

            return (secretAttributes, secretBundle);
        }

        [Fact]
        public void DoesNotLoadFilteredItems()
        {
            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1")
                },
                new []
                {
                    CreateSecret("Secret2", "Value2")
                }
            );

            // Act
            using (var provider = new AzureKeyVaultConfigurationProvider(client, VaultUri, new EndsWithOneKeyVaultSecretManager()))
            {
                provider.Load();

                // Assert
                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.Equal(new[] { "Secret1" }, childKeys);
                Assert.Equal("Value1", provider.Get("Secret1"));
            }
        }

        [Fact]
        public void DoesNotLoadDisabledItems()
        {
            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1")
                },
                new []
                {
                    CreateSecret("Secret2", "Value2", () => new SecretAttributes(enabled: false)),
                    CreateSecret("Secret3", "Value3", () => new SecretAttributes(enabled: null)),
                }
            );

            // Act
            using (var provider = new AzureKeyVaultConfigurationProvider(client, VaultUri, new DefaultKeyVaultSecretManager()))
            {
                provider.Load();

                // Assert
                var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
                Assert.Equal(new[] { "Secret1" }, childKeys);
                Assert.Equal("Value1", provider.Get("Secret1"));
                Assert.Throws<InvalidOperationException>(() => provider.Get("Secret2"));
                Assert.Throws<InvalidOperationException>(() => provider.Get("Secret3"));
            }
        }
        
        [Fact]
        public void SupportsReload()
        {
            var updated = DateTime.Now;

            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1", () => new SecretAttributes(enabled: true, updated: updated))
                }
            );

            // Act & Assert
            using (var provider = new AzureKeyVaultConfigurationProvider(client, VaultUri, new DefaultKeyVaultSecretManager()))
            {
                provider.Load();

                Assert.Equal("Value1", provider.Get("Secret1"));

                client.SetPages(
                    new []
                    {
                        CreateSecret("Secret1", "Value2", () => new SecretAttributes(enabled: true, updated: updated.AddSeconds(1)))
                    }
                );

                provider.Load();
                Assert.Equal("Value2", provider.Get("Secret1"));
            }
        }
        
        [Fact]
        public async Task SupportsAutoReload()
        {
            var updated = DateTime.Now;
            int numOfTokensFired = 0;

            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1", () => new SecretAttributes(enabled: true, updated: updated))
                }
            );

            // Act & Assert
            using (var provider = new ReloadControlKeyVaultProvider(client, VaultUri, new DefaultKeyVaultSecretManager(), reloadPollDelay: NoReloadDelay))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.Equal("Value1", provider.Get("Secret1"));

                await provider.Wait();
            
                client.SetPages(
                    new []
                    {
                        CreateSecret("Secret1", "Value2", () => new SecretAttributes(enabled: true, updated: updated.AddSeconds(1)))
                    }
                );

                provider.Release();

                await provider.Wait();

                Assert.Equal("Value2", provider.Get("Secret1"));
                Assert.Equal(1, numOfTokensFired);
            }
        }

        [Fact]
        public async Task DoesntReloadUnchanged()
        {
            var updated = DateTime.Now;
            int numOfTokensFired = 0;

            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1", () => new SecretAttributes(enabled: true, updated: updated))
                }
            );

            // Act & Assert
            using (var provider = new ReloadControlKeyVaultProvider(client, VaultUri, new DefaultKeyVaultSecretManager(), reloadPollDelay: NoReloadDelay))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.Equal("Value1", provider.Get("Secret1"));

                await provider.Wait();

                provider.Release();

                await provider.Wait();

                Assert.Equal("Value1", provider.Get("Secret1"));
                Assert.Equal(0, numOfTokensFired);
            }
        }

        [Fact]
        public async Task SupportsReloadOnRemove()
        {
            int numOfTokensFired = 0;

            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1"),
                    CreateSecret("Secret2", "Value2")
                }
            );

            // Act & Assert
            using (var provider = new ReloadControlKeyVaultProvider(client, VaultUri, new DefaultKeyVaultSecretManager(), reloadPollDelay: NoReloadDelay))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.Equal("Value1", provider.Get("Secret1"));

                await provider.Wait();
            
                client.SetPages(
                    new []
                    {
                        CreateSecret("Secret1", "Value2")
                    }
                );

                provider.Release();

                await provider.Wait();

                Assert.Throws<InvalidOperationException>(() => provider.Get("Secret2"));
                Assert.Equal(1, numOfTokensFired);
            }
        }

        [Fact]
        public async Task SupportsReloadOnEnabledChange()
        {
            int numOfTokensFired = 0;

            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1"),
                    CreateSecret("Secret2", "Value2")
                }
            );

            // Act & Assert
            using (var provider = new ReloadControlKeyVaultProvider(client, VaultUri, new DefaultKeyVaultSecretManager(), reloadPollDelay: NoReloadDelay))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.Equal("Value1", provider.Get("Secret1"));

                await provider.Wait();
            
                client.SetPages(
                    new []
                    {
                        CreateSecret("Secret1", "Value2"),
                        CreateSecret("Secret2", "Value2", () => new SecretAttributes(enabled: false))
                    }
                );

                provider.Release();

                await provider.Wait();

                Assert.Throws<InvalidOperationException>(() => provider.Get("Secret2"));
                Assert.Equal(1, numOfTokensFired);
            }
        }

        [Fact]
        public async Task SupportsReloadOnAdd()
        {
            int numOfTokensFired = 0;

            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1")
                }
            );

            // Act & Assert
            using (var provider = new ReloadControlKeyVaultProvider(client, VaultUri, new DefaultKeyVaultSecretManager(), reloadPollDelay: NoReloadDelay))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                Assert.Equal("Value1", provider.Get("Secret1"));

                await provider.Wait();
            
                client.SetPages(
                    new []
                    {
                        CreateSecret("Secret1", "Value1"),
                    },
                    new []
                    {
                        CreateSecret("Secret2", "Value2")
                    }
                );

                provider.Release();

                await provider.Wait();
                
                Assert.Equal("Value1", provider.Get("Secret1"));
                Assert.Equal("Value2", provider.Get("Secret2"));
                Assert.Equal(1, numOfTokensFired);
            }
        }

        [Fact]
        public void ReplaceDoubleMinusInKeyName()
        {
            var client = new MockKeyVaultClient();
            client.SetPages(
                new []
                {
                    CreateSecret("Section--Secret1", "Value1")
                }
            );

            // Act
            using (var provider = new AzureKeyVaultConfigurationProvider(client, VaultUri, new DefaultKeyVaultSecretManager()))
            {
                provider.Load();

                // Assert
                Assert.Equal("Value1", provider.Get("Section:Secret1"));
            }
        }

        [Fact]
        public async Task LoadsSecretsInParallel()
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var expectedCount = 2;

            var client = new Mock<MockKeyVaultClient>();
            
            client.Setup(c => c.GetSecretAsync(It.IsAny<string>()))
                .Callback(async (string id) => {
                    if (Interlocked.Decrement(ref expectedCount) == 0)
                    {
                        tcs.SetResult(null);
                    }

                    await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                }).CallBase();

            client.CallBase = true;

            client.Object.SetPages(
                new []
                {
                    CreateSecret("Secret1", "Value1"),
                    CreateSecret("Secret2", "Value2")
                }
            );

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();
            await tcs.Task;

            // Assert
            Assert.Equal("Value1", provider.Get("Secret1"));
            Assert.Equal("Value2", provider.Get("Secret2"));
        }

        [Fact]
        public void ConstructorThrowsForNullManager()
        {
            Assert.Throws<ArgumentNullException>(() => new AzureKeyVaultConfigurationProvider(Mock.Of<IKeyVaultClient>(), VaultUri, null));
        }

        [Fact]
        public void ConstructorThrowsForZeroRefreshPeriodValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AzureKeyVaultConfigurationProvider(new MockKeyVaultClient(), VaultUri, new DefaultKeyVaultSecretManager(), TimeSpan.Zero));
        }

        [Fact]
        public void ConstructorThrowsForNegativeRefreshPeriodValue()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AzureKeyVaultConfigurationProvider(new MockKeyVaultClient(), VaultUri, new DefaultKeyVaultSecretManager(), TimeSpan.FromMilliseconds(-1)));
        }

        [Fact]
        public override void Null_values_are_included_in_the_config()
        {
            AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.NullsTestConfig)), expectNulls: true);
        }

        private class EndsWithOneKeyVaultSecretManager : DefaultKeyVaultSecretManager
        {
            public override bool Load(SecretItem secret)
            {
                return secret.Identifier.Name.EndsWith("1");
            }
        }

        private class PageMock: IPage<SecretItem>
        {
            public IEnumerable<SecretItem> Value { get; set; }

            public IEnumerator<SecretItem> GetEnumerator()
            {
                return Value.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public string NextPageLink { get; set; }
        }

        public class MockKeyVaultClient: IKeyVaultClient
        {
            private (SecretAttributes attributes, SecretBundle bundle)[][] _pages;

            public virtual Task<IPage<SecretItem>> GetSecretsAsync(string vault)
            {
                return GetSecretsNextAsync("0");
            }

            public virtual Task<IPage<SecretItem>> GetSecretsNextAsync(string nextLink)
            {
                var i = int.Parse(nextLink);
                return Task.FromResult((IPage<SecretItem>)new PageMock { NextPageLink = GetNextPageId(i), Value = ToSecrets(_pages[i]) });
            }

            public virtual Task<SecretBundle> GetSecretAsync(string secretIdentifier)
            {
                foreach (var page in _pages)
                {
                    foreach (var secret in page)
                    {
                        if (secret.bundle.Id == secretIdentifier)
                        {
                            return Task.FromResult(secret.bundle);
                        }
                    }
                }

                throw new InvalidOperationException("Secret not found");
            }

            public void SetPages(params (SecretAttributes attributes, SecretBundle bundle)[][] pages)
            {
                _pages = pages;
            }

            private string GetNextPageId(int i)
            {
                var nextPageId = i + 1;
                return _pages.Length > nextPageId ? nextPageId.ToString() : null;
            }

            private IEnumerable<SecretItem> ToSecrets((SecretAttributes attributes, SecretBundle bundle)[] valueTuple)
            {
                foreach (var tuple in valueTuple)
                {
                    yield return new SecretItem(tuple.bundle.Id, tuple.attributes);
                }
            }

        }

        private class ReloadControlKeyVaultProvider : AzureKeyVaultConfigurationProvider
        {
            private TaskCompletionSource<object> _releaseTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            private TaskCompletionSource<object> _signalTaskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            public ReloadControlKeyVaultProvider(IKeyVaultClient client, string vault, IKeyVaultSecretManager manager, TimeSpan? reloadPollDelay = null) : base(client, vault, manager, reloadPollDelay)
            {
            }

            protected override async Task WaitForReload()
            {
                _signalTaskCompletionSource.SetResult(null);
                await _releaseTaskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }

            public async Task Wait()
            {
                await _signalTaskCompletionSource.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }

            public void Release()
            {
                if (!_signalTaskCompletionSource.Task.IsCompleted)
                {
                    throw new InvalidOperationException("Provider is not waiting for reload");
                }

                var releaseTaskCompletionSource = _releaseTaskCompletionSource;
                _releaseTaskCompletionSource = new TaskCompletionSource<object>();
                _signalTaskCompletionSource = new TaskCompletionSource<object>();
                releaseTaskCompletionSource.SetResult(null);
            }
        }

        protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(TestSection testConfig)
        {   
            var values = new List<KeyValuePair<string, string>>();
            SectionToValues(testConfig, "", values);

            var client = new MockKeyVaultClient();
            client.SetPages(values.Select(kvp=>CreateSecret(kvp.Key, kvp.Value)).ToArray());

            return (new AzureKeyVaultConfigurationProvider(client, VaultUri, new DefaultKeyVaultSecretManager()), () => {});
        }
    }
}
