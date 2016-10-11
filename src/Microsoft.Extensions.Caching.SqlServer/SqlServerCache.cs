// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.SqlServer
{
    /// <summary>
    /// Distributed cache implementation using Microsoft SQL Server database.
    /// </summary>
    public class SqlServerCache : IDistributedCache
    {
        private static readonly TimeSpan MinimumExpiredItemsDeletionInterval = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan DefaultExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);

        private readonly IDatabaseOperations _dbOperations;
        private readonly ISystemClock _systemClock;
        private readonly TimeSpan _expiredItemsDeletionInterval;
        private DateTimeOffset _lastExpirationScan;
        private readonly Action _deleteExpiredCachedItemsDelegate;
        private readonly TimeSpan _defaultSlidingExpiration;
        private readonly ILogger _logger;

        public SqlServerCache(IOptions<SqlServerCacheOptions> options)
            : this(options, loggerFactory: null)
        { }

        public SqlServerCache(IOptions<SqlServerCacheOptions> options, ILoggerFactory loggerFactory)
        {
            var cacheOptions = options.Value;

            if (string.IsNullOrEmpty(cacheOptions.ConnectionString))
            {
                throw new ArgumentException(
                    $"{nameof(SqlServerCacheOptions.ConnectionString)} cannot be empty or null.");
            }
            if (string.IsNullOrEmpty(cacheOptions.SchemaName))
            {
                throw new ArgumentException(
                    $"{nameof(SqlServerCacheOptions.SchemaName)} cannot be empty or null.");
            }
            if (string.IsNullOrEmpty(cacheOptions.TableName))
            {
                throw new ArgumentException(
                    $"{nameof(SqlServerCacheOptions.TableName)} cannot be empty or null.");
            }
            if (cacheOptions.ExpiredItemsDeletionInterval.HasValue &&
                cacheOptions.ExpiredItemsDeletionInterval.Value < MinimumExpiredItemsDeletionInterval)
            {
                throw new ArgumentException(
                    $"{nameof(SqlServerCacheOptions.ExpiredItemsDeletionInterval)} cannot be less the minimum " +
                    $"value of {MinimumExpiredItemsDeletionInterval.TotalMinutes} minutes.");
            }
            if (cacheOptions.DefaultSlidingExpiration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cacheOptions.DefaultSlidingExpiration),
                    cacheOptions.DefaultSlidingExpiration,
                    "The sliding expiration value must be positive.");
            }

            _logger = loggerFactory?.CreateLogger<SqlServerCache>();
            _systemClock = cacheOptions.SystemClock ?? new SystemClock();
            _expiredItemsDeletionInterval =
                cacheOptions.ExpiredItemsDeletionInterval ?? DefaultExpiredItemsDeletionInterval;
            _deleteExpiredCachedItemsDelegate = DeleteExpiredCacheItems;
            _defaultSlidingExpiration = cacheOptions.DefaultSlidingExpiration;

            // SqlClient library on Mono doesn't have support for DateTimeOffset and also
            // it doesn't have support for apis like GetFieldValue, GetFieldValueAsync etc.
            // So we detect the platform to perform things differently for Mono vs. non-Mono platforms.
            if (PlatformHelper.IsMono)
            {
                _dbOperations = new MonoDatabaseOperations(
                    cacheOptions.ConnectionString,
                    cacheOptions.SchemaName,
                    cacheOptions.TableName,
                    _systemClock);
            }
            else
            {
                _dbOperations = new DatabaseOperations(
                    cacheOptions.ConnectionString,
                    cacheOptions.SchemaName,
                    cacheOptions.TableName,
                    _systemClock);
            }
        }

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                var value = _dbOperations.GetCacheItem(key);

                ScanForExpiredItemsIfRequired();

                return value;
            }
            catch (Exception ex)
            {
                LogSuppressedException(ex);
            }

            return null;
        }

        public async Task<byte[]> GetAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                var value = await _dbOperations.GetCacheItemAsync(key);

                ScanForExpiredItemsIfRequired();

                return value;
            }
            catch (Exception ex)
            {
                LogSuppressedException(ex);
            }

            return null;
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                _dbOperations.RefreshCacheItem(key);

                ScanForExpiredItemsIfRequired();
            }
            catch (Exception ex)
            {
                LogSuppressedException(ex);
            }
        }

        public async Task RefreshAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                await _dbOperations.RefreshCacheItemAsync(key);

                ScanForExpiredItemsIfRequired();
            }
            catch (Exception ex)
            {
                LogSuppressedException(ex);
            }
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                _dbOperations.DeleteCacheItem(key);

                ScanForExpiredItemsIfRequired();
            }
            catch (Exception ex)
            {
                LogSuppressedException(ex);
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            try
            {
                await _dbOperations.DeleteCacheItemAsync(key);

                ScanForExpiredItemsIfRequired();
            }
            catch (Exception ex)
            {
                LogSuppressedException(ex);
            }
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            GetOptions(ref options);
            try
            {
                _dbOperations.SetCacheItem(key, value, options);

                ScanForExpiredItemsIfRequired();
            }
            catch (Exception ex)
            {
                LogSuppressedException(ex);
            }
        }

        public async Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            GetOptions(ref options);

            try
            {
                await _dbOperations.SetCacheItemAsync(key, value, options);

                ScanForExpiredItemsIfRequired();
            }
            catch (Exception ex)
            {
                LogSuppressedException(ex);
            }
        }

        // Called by multiple actions to see how long it's been since we last checked for expired items.
        // If sufficient time has elapsed then a scan is initiated on a background task.
        private void ScanForExpiredItemsIfRequired()
        {
            var utcNow = _systemClock.UtcNow;
            // TODO: Multiple threads could trigger this scan which leads to multiple calls to database.
            if ((utcNow - _lastExpirationScan) > _expiredItemsDeletionInterval)
            {
                _lastExpirationScan = utcNow;
                Task.Run(_deleteExpiredCachedItemsDelegate);
            }
        }

        private void DeleteExpiredCacheItems()
        {
            _dbOperations.DeleteExpiredCacheItems();
        }

        private void GetOptions(ref DistributedCacheEntryOptions options)
        {
            if (!options.AbsoluteExpiration.HasValue
                && !options.AbsoluteExpirationRelativeToNow.HasValue
                && !options.SlidingExpiration.HasValue)
            {
                options = new DistributedCacheEntryOptions()
                {
                    SlidingExpiration = _defaultSlidingExpiration
                };
            }
        }

        private void LogSuppressedException(Exception ex)
            => _logger?.ExceptionSuppressed(ex);
    }
}