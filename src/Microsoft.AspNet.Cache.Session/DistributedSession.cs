using System;
using System.Diagnostics;
using System.Text;
using Microsoft.Framework.Cache.Distributed;

namespace Microsoft.AspNet.Cache.Session
{
    public class DistributedSession : ISession
    {
        private const byte SerializationRevision = 1;

        private readonly IDistributedCache _cache;
        private readonly string _key;
        private readonly SessionCollection _collection;
        private readonly TimeSpan _idleTimeout;
        private bool _loaded;

        public DistributedSession([NotNull] IDistributedCache cache, [NotNull] string key, TimeSpan idleTimeout, [NotNull] Func<bool> tryEstablishSession)
        {
            _cache = cache;
            _key = key;
            _idleTimeout = idleTimeout;
            _collection = new SessionCollection(tryEstablishSession);
        }

        public ISessionCollection Collection
        {
            get
            {
                Load();
                return _collection;
            }
        }

        public void Load()
        {
            if (!_loaded)
            {
                byte[] data;
                if (_cache.TryGetValue(_key, out data))
                {
                    Deserialize(data);
                }
                _loaded = true;
            }
        }

        public void Commit()
        {
            if (_collection.IsModified)
            {
                _collection.IsModified = false;
                _cache.Set(_key, context => {
                    context.SetSlidingExpiration(_idleTimeout);
                    return Serialize();
                });
            }
        }

        public bool TryCommitIfNotModifiedElsewhere()
        {
            throw new NotImplementedException();
        }

        // Format:
        // byte (1, 0-255): Serialization revision
        // umed (3, 0-16m): entry count
        // foreach entry:
        //  ushort (2, 0-64k): key name byte length
        //  byte[]: utf-8 encoded key name
        //  uint (4, 0-4b): data byte length
        //  byte[]: data
        private byte[] Serialize()
        {
            var builder = new BufferBuilder();
            builder.Add(SerializationRevision);
            builder.Add(SerializeNumAs3Bytes(_collection.Count));

            foreach (var entry in _collection)
            {
                var serializedKey = Encoding.UTF8.GetBytes(entry.Key);
                builder.Add(SerializeNumAs2Bytes(serializedKey.Length));
                builder.Add(serializedKey);
                builder.Add(SerializeNumAs4Bytes(entry.Value.Length));
                builder.Add(entry.Value);
            }

            return builder.Build();
        }

        private void Deserialize(byte[] content)
        {
            if (content == null || content.Length < 4 || content[0] != SerializationRevision)
            {
                // TODO: Throw?
                // Replace the un-readable format.
                _collection.IsModified = true;
                return;
            }

            int offset = 1;
            int expectedEntries = DeserializeNumFrom3Bytes(content, offset: ref offset);
            for (int i = 0; i < expectedEntries; i++)
            {
                int keyLength = DeserializeNumFrom2Bytes(content, offset: ref offset);
                var key = Encoding.UTF8.GetString(content, offset, keyLength);
                offset += keyLength;
                int dataLength = DeserializeNumFrom4Bytes(content, offset: ref offset);
                byte[] data = new byte[dataLength];
                Buffer.BlockCopy(content, offset, data, 0, dataLength);
                offset += dataLength;
                _collection.SetInternal(key, data);
            }

            Debug.Assert(offset == content.Length, "De-serialization length mismatch");
        }

        private byte[] SerializeNumAs2Bytes(int num)
        {
            if (num < 0 || ushort.MaxValue < num)
            {
                throw new ArgumentOutOfRangeException("num", num, "The value cannot be serialized in two bytes.");
            }
            return new byte[]
            {
                (byte)(num >> 8),
                (byte)(0xFF & num)
            };
        }

        private int DeserializeNumFrom2Bytes(byte[] content, ref int offset)
        {
            if (content.Length - offset < 2)
            {
                throw new ArgumentException("Insufficient data remaining", "content");
            }
            return content[offset++] << 8 | content[offset++];
        }

        private byte[] SerializeNumAs3Bytes(int num)
        {
            if (num < 0 || 0xFFFFFF < num)
            {
                throw new ArgumentOutOfRangeException("num", num, "The value cannot be serialized in three bytes.");
            }
            return new byte[]
            {
                (byte)(num >> 16),
                (byte)(0xFF & (num >> 8)),
                (byte)(0xFF & num)
            };
        }

        private int DeserializeNumFrom3Bytes(byte[] content, ref int offset)
        {
            if (content.Length - offset < 3)
            {
                throw new ArgumentException("Insufficient data remaining", "content");
            }
            return content[offset++] << 16 | content[offset++] << 8 | content[offset++];
        }

        private byte[] SerializeNumAs4Bytes(int num)
        {
            if (num < 0)
            {
                throw new ArgumentOutOfRangeException("num", num, "The value cannot be negative.");
            }
            return new byte[]
            {
                (byte)(num >> 24),
                (byte)(0xFF & (num >> 16)),
                (byte)(0xFF & (num >> 8)),
                (byte)(0xFF & num)
            };
        }

        private int DeserializeNumFrom4Bytes(byte[] content, ref int offset)
        {
            if (content.Length - offset < 4)
            {
                throw new ArgumentException("Insufficient data remaining", "content");
            }
            return content[offset++] << 24 | content[offset++] << 16 | content[offset++] << 8 | content[offset++];
        }
    }
}