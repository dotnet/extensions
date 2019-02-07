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

namespace Microsoft.Extensions.Configuration.AzureKeyVault.Test
{
    public class AzureKeyVaultConfigurationTest
    {
        private const string VaultUri = "https://vault";
        private const int _msDelay = 200;
        private const int _retries = 10;

        [Fact]
        public void LoadsAllSecretsFromVault()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("Secret2");

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
            {
                NextPageLink = "next",
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = new SecretAttributes { Enabled = true } } }
            });

            client.Setup(c => c.GetSecretsNextAsync("next")).ReturnsAsync(new PageMock()
            {
                Value = new[] { new SecretItem { Id = secret2Id, Attributes = new SecretAttributes { Enabled = true } } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).ReturnsAsync(new SecretBundle() { Value = "Value1", Id = secret1Id });
            client.Setup(c => c.GetSecretAsync(secret2Id)).ReturnsAsync(new SecretBundle() { Value = "Value2", Id = secret2Id });

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();

            // Assert
            client.VerifyAll();

            var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
            Assert.Equal(new[] { "Secret1", "Secret2" }, childKeys);
            Assert.Equal("Value1", provider.Get("Secret1"));
            Assert.Equal("Value2", provider.Get("Secret2"));
        }

        [Fact]
        public void DoesNotLoadFilteredItems()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("Secret2");

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
            {
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = new SecretAttributes { Enabled = true } }, new SecretItem { Id = secret2Id, Attributes = new SecretAttributes { Enabled = true } } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).ReturnsAsync(new SecretBundle() { Value = "Value1", Id = secret1Id });

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new EndsWithOneKeyVaultSecretManager());
            provider.Load();

            // Assert
            client.VerifyAll();

            var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
            Assert.Equal(new[] { "Secret1" }, childKeys);
            Assert.Equal("Value1", provider.Get("Secret1"));
        }

        [Fact]
        public void DoesNotLoadDisabledItems()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("Secret2");
            var secret3Id = GetSecretId("Secret3");
            var secret4Id = GetSecretId("Secret4");

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
            {
                NextPageLink = "next",
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = new SecretAttributes { Enabled = true } } }
            });

            client.Setup(c => c.GetSecretsNextAsync("next")).ReturnsAsync(new PageMock()
            {
                Value = new[]
                {
                    new SecretItem { Id = secret2Id, Attributes = new SecretAttributes { Enabled = false } },
                    new SecretItem { Id = secret3Id, Attributes = new SecretAttributes { Enabled = null } },
                    new SecretItem { Id = secret4Id, Attributes = null },
                }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).ReturnsAsync(new SecretBundle() { Value = "Value1", Id = secret1Id });

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();

            // Assert
            client.VerifyAll();

            var childKeys = provider.GetChildKeys(Enumerable.Empty<string>(), null).ToArray();
            Assert.Equal(new[] { "Secret1" }, childKeys);
            Assert.Equal("Value1", provider.Get("Secret1"));
            Assert.Throws<InvalidOperationException>(() => provider.Get("Secret2"));
            Assert.Throws<InvalidOperationException>(() => provider.Get("Secret3"));
            Assert.Throws<InvalidOperationException>(() => provider.Get("Secret4"));
        }

        [Fact]
        public void SupportsReload()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var value = "Value1";

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
            {
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = new SecretAttributes { Enabled = true } } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).Returns((string id) => Task.FromResult(new SecretBundle() { Value = value, Id = id }));

            // Act & Assert
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();

            client.VerifyAll();
            Assert.Equal("Value1", provider.Get("Secret1"));

            value = "Value2";
            provider.Load();
            Assert.Equal("Value2", provider.Get("Secret1"));
        }

        [Fact]
        public async Task SupportsReloadOnRemove()
        {
            const int expectedNumOfTokensFired = 2;
            int numOfTokensFired = 0;

            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("secret2");
            var value1 = "Value1";
            var value2 = "Value2";


            DateTime time = new DateTime(100);
            SecretAttributes secretAttribute = new SecretAttributes(true, null, null, null, time, null);
            SecretAttributes anotherSecretAttribute = new SecretAttributes(true, null, null, null, time, null);

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
            {
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = secretAttribute },
                                new SecretItem { Id = secret2Id, Attributes = anotherSecretAttribute }}
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).Returns((string id) => Task.FromResult(new SecretBundle()
            { Value = value1, Id = id }
            ));

            client.Setup(c => c.GetSecretAsync(secret2Id)).Returns((string id) => Task.FromResult(new SecretBundle()
            { Value = value2, Id = id }
            ));

            // Act & Assert
            TimeSpan delay = new TimeSpan(0, 0, 0, 0, 10);

            using (var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager(), delay))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                client.VerifyAll();
                Assert.Equal("Value1", provider.Get("Secret1"));
                Assert.Equal("Value2", provider.Get("Secret2"));

                // Remove one secret
                client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
                {
                    Value = new[] { new SecretItem { Id = secret1Id, Attributes = secretAttribute }}
                });

                // Verfy reloadToken was recieved
                var token = provider.GetReloadToken();
                await WaitForTokenChange(token, "Reload token never changed when key vault updated.");

            }

            Assert.Equal(expectedNumOfTokensFired, numOfTokensFired);
        }

        [Fact]
        public async Task SupportsReloadOnAdd()
        {
            const int expectedNumOfTokensFired = 2;
            int numOfTokensFired = 0;

            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("secret2");
            var value1 = "Value1";
            var value2 = "Value2";


            DateTime time = new DateTime(100);
            SecretAttributes secretAttribute = new SecretAttributes(true, null, null, null, time, null);
            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
            {
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = secretAttribute } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).Returns((string id) => Task.FromResult(new SecretBundle()
            { Value = value1, Id = id }
            ));

            // Act & Assert
            TimeSpan delay = new TimeSpan(0, 0, 0, 0, 10);

            using (var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager(), delay))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                client.VerifyAll();
                Assert.Equal("Value1", provider.Get("Secret1"));

                // Add a new record
                SecretAttributes anotherSecretAttribute = new SecretAttributes(true, null, null, null, time, null);
                client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
                {
                    Value = new[] { new SecretItem { Id = secret2Id, Attributes = anotherSecretAttribute },
                                    new SecretItem { Id = secret1Id, Attributes = secretAttribute } }
                });

                client.Setup(c => c.GetSecretAsync(secret2Id)).Returns((string id) => Task.FromResult(new SecretBundle()
                { Value = value2, Id = id }
                ));

                var token = provider.GetReloadToken();
                await WaitForTokenChange(token, "Reload token never changed when key vault updated.");

                Assert.Equal("Value2", provider.Get("Secret2"));
            }

            Assert.Equal(expectedNumOfTokensFired, numOfTokensFired);
        }

        [Fact]
        public async Task SupportsReloadOnChange()
        {
            const int expectedNumOfTokensFired = 2;
            int numOfTokensFired = 0;

            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var value = "Value1";

            DateTime time = new DateTime(100);
            SecretAttributes secretAttribute = new SecretAttributes(true, null, null, null, time, null);
            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock() {
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = secretAttribute } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).Returns((string id) => Task.FromResult(new SecretBundle() 
                { Value = value, Id = id }
            ));

            // Act & Assert
            TimeSpan delay = new TimeSpan(0,0,0,0,10);

            using (var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager(), delay))
            {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                client.VerifyAll();
                Assert.Equal("Value1", provider.Get("Secret1"));

                // update the record
                SecretAttributes secretAttributeUpdated = new SecretAttributes(true, null, null, null, time.AddTicks(delay.Milliseconds * 10), null);
                client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
                {
                    Value = new[] { new SecretItem { Id = secret1Id, Attributes = secretAttributeUpdated } }
                });

                var token = provider.GetReloadToken();
                value = "Value2";

                await WaitForTokenChange(token, "Reload token never changed when key vault updated.");

                Assert.Equal("Value2", provider.Get("Secret1"));
            }

            Assert.Equal(expectedNumOfTokensFired, numOfTokensFired);
        }

        [Fact]
        public async Task DoesNotReloadWithNoChange() {
            const int expectedNumOfTokensFired = 1;
            int numOfTokensFired = 0;

            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var value = "Value1";

            DateTime time = new DateTime(100);
            SecretAttributes secretAttribute = new SecretAttributes(true, null, null, null, time, null);
            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock() {
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = secretAttribute } }
            });
            client.Setup(c => c.GetSecretAsync(secret1Id)).Returns((string id) => Task.FromResult(new SecretBundle() { Value = value, Id = id }
            ));

            // Act & Assert
            TimeSpan delay = new TimeSpan(0, 0, 0, 0, 10);

            using (var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager(), delay)) {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                client.VerifyAll();
                Assert.Equal("Value1", provider.Get("Secret1"));

                var token = provider.GetReloadToken();

                await Assert.ThrowsAnyAsync<Exception>(async () => await WaitForTokenChange(token, "Reload token never changed when key vault updated."));

                Assert.Equal("Value1", provider.Get("Secret1"));
            }

            Assert.Equal(expectedNumOfTokensFired, numOfTokensFired);
        }

        [Fact]
        public async Task SupportsReloadingOnMultiplePages() {
            const int expectedNumOfTokensFired = 2;
            int numOfTokensFired = 0;

            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("Secret2");
            var secret1Value = "Value1";
            var secret2Value = "Value2";
            var nextPageLink = "page2";

            DateTime time = new DateTime(100);
            SecretAttributes secret1Attribute = new SecretAttributes(true, null, null, null, time, null);
            SecretAttributes secret2Attribute = new SecretAttributes(true, null, null, null, time, null);
            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock() {
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = secret1Attribute } },
                NextPageLink = nextPageLink
            });
            client.Setup(c => c.GetSecretsNextAsync(nextPageLink)).ReturnsAsync(new PageMock() {
                Value = new[] { new SecretItem { Id = secret2Id, Attributes = secret2Attribute } },
            });
            client.Setup(c => c.GetSecretAsync(secret1Id)).Returns((string id) => Task.FromResult(new SecretBundle() { Value = secret1Value, Id = id }
            ));
            client.Setup(c => c.GetSecretAsync(secret2Id)).Returns((string id) => Task.FromResult(new SecretBundle() { Value = secret2Value, Id = id }
            ));


            // Act & Assert
            TimeSpan delay = new TimeSpan(0, 0, 0, 0, 10);

            using (var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager(), delay)) {
                ChangeToken.OnChange(
                    () => provider.GetReloadToken(),
                    () => {
                        numOfTokensFired++;
                    });

                provider.Load();

                client.VerifyAll();
                Assert.Equal("Value1", provider.Get("Secret1"));
                Assert.Equal("Value2", provider.Get("Secret2"));

                var token = provider.GetReloadToken();

                // update the record
                secret2Value = "Value100";
                SecretAttributes secretAttributeUpdated = new SecretAttributes(true, null, null, null, time.AddTicks(delay.Milliseconds * 10), null);
                client.Setup(c => c.GetSecretsNextAsync(nextPageLink)).ReturnsAsync(new PageMock() {
                    Value = new[] { new SecretItem { Id = secret2Id, Attributes = secretAttributeUpdated } }
                });
                
                await WaitForTokenChange(token, "Reload token never changed when key vault updated.");

                Assert.Equal("Value1", provider.Get("Secret1"));
                Assert.Equal("Value100", provider.Get("Secret2"));
            }

            Assert.Equal(expectedNumOfTokensFired, numOfTokensFired);
        }

        [Fact]
        public void ReplaceDoubleMinusInKeyName()
        {
            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Section--Secret1");

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
            {
                Value = new[] { new SecretItem { Id = secret1Id, Attributes = new SecretAttributes { Enabled = true } } }
            });

            client.Setup(c => c.GetSecretAsync(secret1Id)).ReturnsAsync(new SecretBundle() { Value = "Value1", Id = secret1Id });

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();

            // Assert
            client.VerifyAll();

            Assert.Equal("Value1", provider.Get("Section:Secret1"));
        }

        [Fact]
        public async Task LoadsSecretsInParallel()
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var expectedCount = 2;

            var client = new Mock<IKeyVaultClient>(MockBehavior.Strict);
            var secret1Id = GetSecretId("Secret1");
            var secret2Id = GetSecretId("Secret2");

            client.Setup(c => c.GetSecretsAsync(VaultUri)).ReturnsAsync(new PageMock()
            {
                Value = new[]
                {
                    new SecretItem { Id = secret1Id, Attributes = new SecretAttributes { Enabled = true } },
                    new SecretItem { Id = secret2Id, Attributes = new SecretAttributes { Enabled = true } }
                }
            });


            client.Setup(c => c.GetSecretAsync(It.IsAny<string>()))
                .Returns(async (string id) => {
                    var shortId = id.Substring(id.LastIndexOf('/') + 1);
                    if (Interlocked.Decrement(ref expectedCount) == 0)
                    {
                        tcs.SetResult(null);
                    }

                    await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
                    return new SecretBundle() { Value = "Value" + shortId, Id = id };
                });

            // Act
            var provider = new AzureKeyVaultConfigurationProvider(client.Object, VaultUri, new DefaultKeyVaultSecretManager());
            provider.Load();
            await tcs.Task;

            // Assert
            client.VerifyAll();

            Assert.Equal("ValueSecret1", provider.Get("Secret1"));
            Assert.Equal("ValueSecret2", provider.Get("Secret2"));
        }

        [Fact]
        public void ConstructorThrowsForNullManager()
        {
            Assert.Throws<ArgumentNullException>(() => new AzureKeyVaultConfigurationProvider(Mock.Of<IKeyVaultClient>(), VaultUri, null));
        }

        private string GetSecretId(string name) => new SecretIdentifier(VaultUri, name).Identifier;

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

        private async Task WaitForTokenChange(
            IChangeToken token,
            string failureMessage,
            int multiplier = 1) {
            var i = 0;
            while (!token.HasChanged) {
                if (++i >= _retries * multiplier) {
                    throw new Exception(failureMessage);
                }

                await Task.Delay(_msDelay);
                }
        }
    }
}
