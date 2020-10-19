using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Microsoft.Extensions.Caching.StackExchangeRedis
{
    /// <summary>
    /// Implements the previous default behavior for RedisCache
    /// </summary>
    public sealed class RedisConnectionManager : IDisposable
    {
        private volatile ConnectionMultiplexer _connection;
        private readonly RedisCacheOptions _options;
        private bool _disposed;

        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);

        public RedisConnectionManager(IOptions<RedisCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

        }

        public ConnectionMultiplexer Connect()
        {
            CheckDisposed();

            if (_connection != null)
            {
                return _connection;
            }

            _connectionLock.Wait();
            try
            {
                if (_connection == null)
                {
                    if (_options.ConfigurationOptions != null)
                    {
                        _connection = ConnectionMultiplexer.Connect(_options.ConfigurationOptions);
                    }
                    else
                    {
                        _connection = ConnectionMultiplexer.Connect(_options.Configuration);
                    }
                }
                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task<ConnectionMultiplexer> ConnectAsync(CancellationToken token = default(CancellationToken))
        {
            token.ThrowIfCancellationRequested();
            CheckDisposed();

            if (_connection != null)
            {
                return _connection;
            }

            await _connectionLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_connection == null)
                {
                    if (_options.ConfigurationOptions != null)
                    {
                        _connection = await ConnectionMultiplexer.ConnectAsync(_options.ConfigurationOptions).ConfigureAwait(false);
                    }
                    else
                    {
                        _connection = await ConnectionMultiplexer.ConnectAsync(_options.Configuration).ConfigureAwait(false);
                    }
                }
                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _connection?.Close();
            _connectionLock?.Dispose();
        }

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(this.GetType().FullName);
            }
        }
    }
}
