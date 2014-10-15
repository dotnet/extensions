using System;
using System.Text;

namespace Microsoft.AspNet.Cache.Session
{
    public static class SessionCollectionExtensions
    {
        public static void SetInt(this ISessionCollection collection, string key, int value)
        {
            var bytes = new byte[]
            {
                (byte)(value >> 24),
                (byte)(0xFF & (value >> 16)),
                (byte)(0xFF & (value >> 8)),
                (byte)(0xFF & value)
            };
            collection.Set(key, bytes);
        }

        public static int? GetInt(this ISessionCollection collection, string key)
        {
            var data = collection.Get(key);
            if (data == null || data.Length < 4)
            {
                return null;
            }
            return data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
        }

        public static void SetString(this ISessionCollection collection, string key, string value)
        {
            collection.Set(key, Encoding.UTF8.GetBytes(value));
        }

        public static string GetString(this ISessionCollection collection, string key)
        {
            var data = collection.Get(key);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data);
        }

        public static byte[] Get(this ISessionCollection collection, string key)
        {
            byte[] value = null;
            collection.TryGetValue(key, out value);
            return value;
        }

        public static void Set(this ISessionCollection collection, string key, byte[] value)
        {
            collection.Set(key, new ArraySegment<byte>(value));
        }
    }
}