using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Microsoft.Extensions.Internal.Benchmarks
{
    public class WebEncodersBenchmarks
    {
        private const int ByteArraySize = 500;
        private readonly byte[] _data;
        private readonly string _dataEncoded;
        private readonly byte[] _dataWithOffset;
        private readonly string _dataWithOffsetEncoded;
        private readonly byte[] _guid;
        private readonly string _guidEncoded;

        public WebEncodersBenchmarks()
        {
            var random = new Random();
            _data = new byte[ByteArraySize];
            random.NextBytes(_data);
            _dataEncoded = WebEncoders.Base64UrlEncode(_data);

            _dataWithOffset = new byte[3].Concat(_data).Concat(new byte[2]).ToArray();
            _dataWithOffsetEncoded = "xx" + _dataEncoded + "yyy";

            _guid = Guid.NewGuid().ToByteArray();
            _guidEncoded = WebEncoders.Base64UrlEncode(_guid);
        }

        [Benchmark]
        public byte[] Base64UrlDecode_Data()
        {
            return WebEncoders.Base64UrlDecode(_dataEncoded);
        }

        [Benchmark]
        public byte[] Base64UrlDecode_DataWithOffset()
        {
            return WebEncoders.Base64UrlDecode(_dataWithOffsetEncoded, 2, _dataEncoded.Length);
        }

        [Benchmark]
        public byte[] Base64UrlDecode_Guid()
        {
            return WebEncoders.Base64UrlDecode(_guidEncoded);
        }

        [Benchmark]
        public string Base64UrlEncode_Data()
        {
            return WebEncoders.Base64UrlEncode(_data);
        }

        [Benchmark]
        public string Base64UrlEncode_DataWithOffset()
        {
            return WebEncoders.Base64UrlEncode(_dataWithOffset, 3, _data.Length);
        }

        [Benchmark]
        public string Base64UrlEncode_Guid()
        {
            return WebEncoders.Base64UrlEncode(_guid);
        }

        [Benchmark]
        public int GetArraySizeRequiredToDecode()
        {
            return WebEncoders.GetArraySizeRequiredToDecode(ByteArraySize);
        }

        [Benchmark]
        public int GetArraySizeRequiredToEncode()
        {
            return WebEncoders.GetArraySizeRequiredToEncode(ByteArraySize);
        }
    }
}
