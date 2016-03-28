// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Caching.SqlServer
{
    // This requires SQL Server database to be setup
    // public
    class SqlServerCacheWithDatabaseTest
    {
        private const string ConnectionStringKey = "ConnectionString";
        private const string SchemaNameKey = "SchemaName";
        private const string TableNameKey = "TableName";

        private readonly string _tableName;
        private readonly string _schemaName;
        private readonly string _connectionString;

        public SqlServerCacheWithDatabaseTest()
        {
            // TODO: Figure how to use config.json which requires resolving IApplicationEnvironment which currently
            // fails.

            var memoryConfigurationData = new Dictionary<string, string>
            {
                { ConnectionStringKey, "Server=localhost;Database=CacheTestDb;Trusted_Connection=True;" },
                { SchemaNameKey, "dbo" },
                { TableNameKey, "CacheTest" },
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder
                .AddInMemoryCollection(memoryConfigurationData)
                .AddEnvironmentVariables();

            var configuration = configurationBuilder.Build();
            _tableName = configuration[TableNameKey];
            _schemaName = configuration[SchemaNameKey];
            _connectionString = configuration[ConnectionStringKey];
        }

        [Fact]
        public async Task ReturnsNullValue_ForNonExistingCacheItem()
        {
            // Arrange
            var sqlServerCache = GetCache();

            // Act
            var value = await sqlServerCache.GetAsync("NonExisting");

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public async Task SetWithAbsoluteExpirationSetInThePast_Throws()
        {
            // Arrange
            var testClock = new TestClock();
            var key = Guid.NewGuid().ToString();
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            var sqlServerCache = GetCache(testClock);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return sqlServerCache.SetAsync(
                    key,
                    expectedValue,
                    new DistributedCacheEntryOptions().SetAbsoluteExpiration(testClock.UtcNow.AddHours(-1)));
            });
            Assert.Equal("The absolute expiration value must be in the future.", exception.Message);
        }

        [Fact]
        public async Task SetCacheItem_SucceedsFor_KeyEqualToMaximumSize()
        {
            // Arrange
            // Create a key with the maximum allowed key length. Here a key of length 898 bytes is created.
            var key = new string('a', SqlParameterCollectionExtensions.CacheItemIdColumnWidth);
            var testClock = new TestClock();
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            var sqlServerCache = GetCache(testClock);

            // Act
            await sqlServerCache.SetAsync(
                key, expectedValue,
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30)));

            // Assert
            var cacheItem = await GetCacheItemFromDatabaseAsync(key);
            Assert.Equal(expectedValue, cacheItem.Value);

            // Act
            await sqlServerCache.RemoveAsync(key);

            // Assert
            var cacheItemInfo = await GetCacheItemFromDatabaseAsync(key);
            Assert.Null(cacheItemInfo);
        }

        [Fact]
        public async Task SetCacheItem_FailsFor_KeyGreaterThanMaximumSize()
        {
            // Arrange
            // Create a key which is greater than the maximum length.
            var key = new string('b', SqlParameterCollectionExtensions.CacheItemIdColumnWidth + 1);
            var testClock = new TestClock();
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            var sqlServerCache = GetCache(testClock);

            // Act
            await sqlServerCache.SetAsync(
                key, expectedValue,
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(30)));

            // Assert
            var cacheItem = await GetCacheItemFromDatabaseAsync(key);
            Assert.Null(cacheItem);
        }

        // Arrange
        [Theory]
        [InlineData(10, 11)]
        [InlineData(10, 30)]
        public async Task SetWithSlidingExpiration_ReturnsNullValue_ForExpiredCacheItem(
            int slidingExpirationWindow, int accessItemAt)
        {
            // Arrange
            var testClock = new TestClock();
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            await sqlServerCache.SetAsync(
                key,
                Encoding.UTF8.GetBytes("Hello, World!"),
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(10)));

            // set the clock's UtcNow far in future
            testClock.Add(TimeSpan.FromHours(10));

            // Act
            var value = await sqlServerCache.GetAsync(key);

            // Assert
            Assert.Null(value);
        }

        [Theory]
        [InlineData(5, 15)]
        [InlineData(10, 20)]
        public async Task SetWithSlidingExpiration_ExtendsExpirationTime(int accessItemAt, int expected)
        {
            // Arrange
            var testClock = new TestClock();
            var slidingExpirationWindow = TimeSpan.FromSeconds(10);
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            var expectedExpirationTime = testClock.UtcNow.AddSeconds(expected);
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions().SetSlidingExpiration(slidingExpirationWindow));

            testClock.Add(TimeSpan.FromSeconds(accessItemAt));
            // Act
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpirationWindow,
                absoluteExpiration: null,
                expectedExpirationTime: expectedExpirationTime);
        }

        [Theory]
        [InlineData(8)]
        [InlineData(50)]
        public async Task SetWithSlidingExpirationAndAbsoluteExpiration_ReturnsNullValue_ForExpiredCacheItem(
            int accessItemAt)
        {
            // Arrange
            var testClock = new TestClock();
            var utcNow = testClock.UtcNow;
            var slidingExpiration = TimeSpan.FromSeconds(5);
            var absoluteExpiration = utcNow.Add(TimeSpan.FromSeconds(20));
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                // Set both sliding and absolute expiration
                new DistributedCacheEntryOptions()
                .SetSlidingExpiration(slidingExpiration)
                .SetAbsoluteExpiration(absoluteExpiration));

            // Act
            utcNow = testClock.Add(TimeSpan.FromSeconds(accessItemAt)).UtcNow;
            var value = await sqlServerCache.GetAsync(key);

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public async Task SetWithAbsoluteExpirationRelativeToNow_ReturnsNullValue_ForExpiredCacheItem()
        {
            // Arrange
            var testClock = new TestClock();
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            await sqlServerCache.SetAsync(
                key,
                Encoding.UTF8.GetBytes("Hello, World!"),
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(relative: TimeSpan.FromSeconds(10)));

            // set the clock's UtcNow far in future
            testClock.Add(TimeSpan.FromHours(10));

            // Act
            var value = await sqlServerCache.GetAsync(key);

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public async Task SetWithAbsoluteExpiration_ReturnsNullValue_ForExpiredCacheItem()
        {
            // Arrange
            var testClock = new TestClock();
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            await sqlServerCache.SetAsync(
                key,
                Encoding.UTF8.GetBytes("Hello, World!"),
                new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(absolute: testClock.UtcNow.Add(TimeSpan.FromSeconds(30))));

            // set the clock's UtcNow far in future
            testClock.Add(TimeSpan.FromHours(10));

            // Act
            var value = await sqlServerCache.GetAsync(key);

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public async Task ThrowsException_OnNoSlidingOrAbsoluteExpirationOptions()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache();
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            {
                return sqlServerCache.SetAsync(
                    key,
                    expectedValue,
                    new DistributedCacheEntryOptions());
            });
            Assert.Equal("Either absolute or sliding expiration needs to be provided.", exception.Message);
        }

        [Fact]
        public async Task DoesNotThrowException_WhenOnlyAbsoluteExpirationSupplied_AbsoluteExpirationRelativeToNow()
        {
            // Arrange
            var testClock = new TestClock();
            var absoluteExpirationRelativeToUtcNow = TimeSpan.FromSeconds(10);
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            var expectedAbsoluteExpiration = testClock.UtcNow.Add(absoluteExpirationRelativeToUtcNow);

            // Act
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(relative: absoluteExpirationRelativeToUtcNow));

            // Assert
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpiration: null,
                absoluteExpiration: expectedAbsoluteExpiration,
                expectedExpirationTime: expectedAbsoluteExpiration);
        }

        [Fact]
        public async Task DoesNotThrowException_WhenOnlyAbsoluteExpirationSupplied_AbsoluteExpiration()
        {
            // Arrange
            var testClock = new TestClock();
            var expectedAbsoluteExpiration = new DateTimeOffset(2025, 1, 1, 1, 0, 0, TimeSpan.Zero);
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache();
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");

            // Act
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(absolute: expectedAbsoluteExpiration));

            // Assert
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpiration: null,
                absoluteExpiration: expectedAbsoluteExpiration,
                expectedExpirationTime: expectedAbsoluteExpiration);
        }

        [Fact]
        public async Task SetCacheItem_UpdatesAbsoluteExpirationTime()
        {
            // Arrange
            var testClock = new TestClock();
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            var absoluteExpiration = testClock.UtcNow.Add(TimeSpan.FromSeconds(10));

            // Act & Assert
            // Creates a new item
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(absoluteExpiration));
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpiration: null,
                absoluteExpiration: absoluteExpiration,
                expectedExpirationTime: absoluteExpiration);

            // Updates an existing item with new absolute expiration time
            absoluteExpiration = testClock.UtcNow.Add(TimeSpan.FromMinutes(30));
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(absoluteExpiration));
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpiration: null,
                absoluteExpiration: absoluteExpiration,
                expectedExpirationTime: absoluteExpiration);
        }

        [Fact]
        public async Task SetCacheItem_WithValueLargerThan_DefaultColumnWidth()
        {
            // Arrange
            var testClock = new TestClock();
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = new byte[SqlParameterCollectionExtensions.DefaultValueColumnWidth + 100];
            var absoluteExpiration = testClock.UtcNow.Add(TimeSpan.FromSeconds(10));

            // Act
            // Creates a new item
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(absoluteExpiration));

            // Assert
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpiration: null,
                absoluteExpiration: absoluteExpiration,
                expectedExpirationTime: absoluteExpiration);
        }

        [Fact]
        public async Task ExtendsExpirationTime_ForSlidingExpiration()
        {
            // Arrange
            var testClock = new TestClock();
            var slidingExpiration = TimeSpan.FromSeconds(10);
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            // The operations Set and Refresh here extend the sliding expiration 2 times.
            var expectedExpiresAtTime = testClock.UtcNow.AddSeconds(15);
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions().SetSlidingExpiration(slidingExpiration));

            // Act
            testClock.Add(TimeSpan.FromSeconds(5));
            await sqlServerCache.RefreshAsync(key);

            // Assert
            // verify if the expiration time in database is set as expected
            var cacheItemInfo = await GetCacheItemFromDatabaseAsync(key);
            Assert.NotNull(cacheItemInfo);
            Assert.Equal(slidingExpiration, cacheItemInfo.SlidingExpirationInSeconds);
            Assert.Null(cacheItemInfo.AbsoluteExpiration);
            Assert.Equal(expectedExpiresAtTime, cacheItemInfo.ExpiresAtTime);
        }

        [Fact]
        public async Task GetItem_SlidingExpirationDoesNot_ExceedAbsoluteExpirationIfSet()
        {
            // Arrange
            var testClock = new TestClock();
            var utcNow = testClock.UtcNow;
            var slidingExpiration = TimeSpan.FromSeconds(5);
            var absoluteExpiration = utcNow.Add(TimeSpan.FromSeconds(20));
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                // Set both sliding and absolute expiration
                new DistributedCacheEntryOptions()
                .SetSlidingExpiration(slidingExpiration)
                .SetAbsoluteExpiration(absoluteExpiration));

            // Act && Assert
            var cacheItemInfo = await GetCacheItemFromDatabaseAsync(key);
            Assert.NotNull(cacheItemInfo);
            Assert.Equal(utcNow.AddSeconds(5), cacheItemInfo.ExpiresAtTime);

            // Accessing item at time...
            utcNow = testClock.Add(TimeSpan.FromSeconds(5)).UtcNow;
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpiration,
                absoluteExpiration,
                expectedExpirationTime: utcNow.AddSeconds(5));

            // Accessing item at time...
            utcNow = testClock.Add(TimeSpan.FromSeconds(5)).UtcNow;
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpiration,
                absoluteExpiration,
                expectedExpirationTime: utcNow.AddSeconds(5));

            // Accessing item at time...
            utcNow = testClock.Add(TimeSpan.FromSeconds(5)).UtcNow;
            // The expiration extension must not exceed the absolute expiration
            await AssertGetCacheItemFromDatabaseAsync(
                sqlServerCache,
                key,
                expectedValue,
                slidingExpiration,
                absoluteExpiration,
                expectedExpirationTime: absoluteExpiration);
        }

        [Fact]
        public async Task DoestNotExtendsExpirationTime_ForAbsoluteExpiration()
        {
            // Arrange
            var testClock = new TestClock();
            var absoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            var expectedExpiresAtTime = testClock.UtcNow.Add(absoluteExpirationRelativeToNow);
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(absoluteExpirationRelativeToNow));
            testClock.Add(TimeSpan.FromSeconds(25));

            // Act
            var value = await sqlServerCache.GetAsync(key);

            // Assert
            Assert.NotNull(value);
            Assert.Equal(expectedValue, value);

            // verify if the expiration time in database is set as expected
            var cacheItemInfo = await GetCacheItemFromDatabaseAsync(key);
            Assert.NotNull(cacheItemInfo);
            Assert.Equal(expectedExpiresAtTime, cacheItemInfo.ExpiresAtTime);
        }

        [Fact]
        public async Task RefreshItem_ExtendsExpirationTime_ForSlidingExpiration()
        {
            // Arrange
            var testClock = new TestClock();
            var slidingExpiration = TimeSpan.FromSeconds(10);
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache(testClock);
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            // The operations Set and Refresh here extend the sliding expiration 2 times.
            var expectedExpiresAtTime = testClock.UtcNow.AddSeconds(15);
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions().SetSlidingExpiration(slidingExpiration));

            // Act
            testClock.Add(TimeSpan.FromSeconds(5));
            await sqlServerCache.RefreshAsync(key);

            // Assert
            // verify if the expiration time in database is set as expected
            var cacheItemInfo = await GetCacheItemFromDatabaseAsync(key);
            Assert.NotNull(cacheItemInfo);
            Assert.Equal(slidingExpiration, cacheItemInfo.SlidingExpirationInSeconds);
            Assert.Null(cacheItemInfo.AbsoluteExpiration);
            Assert.Equal(expectedExpiresAtTime, cacheItemInfo.ExpiresAtTime);
        }

        [Fact]
        public async Task GetCacheItem_IsCaseSensitive()
        {
            // Arrange
            var key = Guid.NewGuid().ToString().ToLower(); // lower case
            var sqlServerCache = GetCache();
            await sqlServerCache.SetAsync(
                key,
                Encoding.UTF8.GetBytes("Hello, World!"),
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(relative: TimeSpan.FromHours(1)));

            // Act
            var value = await sqlServerCache.GetAsync(key.ToUpper()); // key made upper case

            // Assert
            Assert.Null(value);
        }

        [Fact]
        public async Task GetCacheItem_DoesNotTrimTrailingSpaces()
        {
            // Arrange
            var key = string.Format("  {0}  ", Guid.NewGuid()); // with trailing spaces
            var sqlServerCache = GetCache();
            var expectedValue = Encoding.UTF8.GetBytes("Hello, World!");
            await sqlServerCache.SetAsync(
                key,
                expectedValue,
                new DistributedCacheEntryOptions().SetAbsoluteExpiration(relative: TimeSpan.FromHours(1)));

            // Act
            var value = await sqlServerCache.GetAsync(key);

            // Assert
            Assert.NotNull(value);
            Assert.Equal(expectedValue, value);
        }

        [Fact]
        public async Task DeletesCacheItem_OnExplicitlyCalled()
        {
            // Arrange
            var key = Guid.NewGuid().ToString();
            var sqlServerCache = GetCache();
            await sqlServerCache.SetAsync(
                key,
                Encoding.UTF8.GetBytes("Hello, World!"),
                new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromSeconds(10)));

            // Act
            await sqlServerCache.RemoveAsync(key);

            // Assert
            var cacheItemInfo = await GetCacheItemFromDatabaseAsync(key);
            Assert.Null(cacheItemInfo);
        }

        private SqlServerCache GetCache(ISystemClock testClock = null)
        {
            var options = new SqlServerCacheOptions()
            {
                ConnectionString = _connectionString,
                SchemaName = _schemaName,
                TableName = _tableName,
                SystemClock = testClock ?? new TestClock(),
                ExpiredItemsDeletionInterval = TimeSpan.FromHours(2)
            };

            return new SqlServerCache(new TestSqlServerCacheOptions(options));
        }

        private async Task AssertGetCacheItemFromDatabaseAsync(
            SqlServerCache cache,
            string key,
            byte[] expectedValue,
            TimeSpan? slidingExpiration,
            DateTimeOffset? absoluteExpiration,
            DateTimeOffset expectedExpirationTime)
        {
            var value = await cache.GetAsync(key);
            Assert.NotNull(value);
            Assert.Equal(expectedValue, value);
            var cacheItemInfo = await GetCacheItemFromDatabaseAsync(key);
            Assert.NotNull(cacheItemInfo);
            Assert.Equal(slidingExpiration, cacheItemInfo.SlidingExpirationInSeconds);
            Assert.Equal(absoluteExpiration, cacheItemInfo.AbsoluteExpiration);
            Assert.Equal(expectedExpirationTime, cacheItemInfo.ExpiresAtTime);
        }

        private async Task<CacheItemInfo> GetCacheItemFromDatabaseAsync(string key)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var command = new SqlCommand(
                    $"SELECT Id, Value, ExpiresAtTime, SlidingExpirationInSeconds, AbsoluteExpiration " +
                    $"FROM {_tableName} WHERE Id = @Id",
                    connection);
                command.Parameters.AddWithValue("Id", key);

                await connection.OpenAsync();

                var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);

                // NOTE: The following code is made to run on Mono as well because of which
                // we cannot use GetFieldValueAsync etc.
                if (await reader.ReadAsync())
                {
                    var cacheItemInfo = new CacheItemInfo();
                    cacheItemInfo.Id = key;
                    cacheItemInfo.Value = (byte[])reader[1];
                    cacheItemInfo.ExpiresAtTime = DateTimeOffset.Parse(reader[2].ToString());

                    if (!await reader.IsDBNullAsync(3))
                    {
                        cacheItemInfo.SlidingExpirationInSeconds = TimeSpan.FromSeconds(reader.GetInt64(3));
                    }

                    if (!await reader.IsDBNullAsync(4))
                    {
                        cacheItemInfo.AbsoluteExpiration = DateTimeOffset.Parse(reader[4].ToString());
                    }

                    return cacheItemInfo;
                }
                else
                {
                    return null;
                }
            }
        }

        private class TestSqlServerCacheOptions : IOptions<SqlServerCacheOptions>
        {
            private readonly SqlServerCacheOptions _innerOptions;

            public TestSqlServerCacheOptions(SqlServerCacheOptions innerOptions)
            {
                _innerOptions = innerOptions;
            }

            public SqlServerCacheOptions Value
            {
                get
                {
                    return _innerOptions;
                }
            }
        }
    }
}
