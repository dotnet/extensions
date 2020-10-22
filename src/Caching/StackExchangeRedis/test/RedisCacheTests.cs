// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using StackExchange.Redis;
using Xunit;

namespace Microsoft.Extensions.Caching.StackExchangeRedis
{
    public class RedisCacheTests
    {
        [Fact]
        public void Remove_OptionHasConnectionMultiplexer_CallsConnectionMultiplexerGetDatabase()
        {
            // Arrange
            var connectionMultiplexerMock = new Mock<IConnectionMultiplexer>();
            var databaseMock = new Mock<IDatabase>();
            connectionMultiplexerMock.Setup(c => c.GetDatabase(-1, null)).Returns(databaseMock.Object);

            var options = new RedisCacheOptions {
                ConnectionMultiplexer = connectionMultiplexerMock.Object
            };
            var redisCache = new RedisCache(options);
            var key = "key";

            // Act
            redisCache.Remove(key);

            // Assert
            databaseMock.Verify((d) => d.KeyDelete(key, CommandFlags.None), Times.Once);
        }
    }
}
