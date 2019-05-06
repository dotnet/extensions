using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Microsoft.Extensions.Primitives.Performance
{
    public class StringValuesBenchmark
    {
        const int Iterations = 40;

        private readonly string _string = "Hello world!";
        private readonly string[] _stringArray = new[] { "Hello", "world", "!" };
        private readonly StringValues _stringBased = new StringValues("Hello world!");
        private readonly StringValues _arrayBased = new StringValues(new[] { "Hello", "world", "!" });

#pragma warning disable CS0414
        private HeaderReferences _headers;
#pragma warning restore CS0414

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void ClearHeaders()
        {
            for (var i = 0; i < Iterations; i++)
            {
                ClearHeaders_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ClearHeaders_Body()
        {
            _headers = default;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Ctor_String()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Ctor_String_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private StringValues Ctor_String_Body()
        {
            return new StringValues(_string);
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Ctor_Array()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Ctor_Array_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private StringValues Ctor_Array_Body()
        {
            return new StringValues(_stringArray);
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Index_FirstElement_String()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Index_FirstElement_String_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string Index_FirstElement_String_Body()
        {
            return _stringBased[0];
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Index_FirstElement_Array()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Index_FirstElement_Array_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string Index_FirstElement_Array_Body()
        {
            return _arrayBased[0];
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Count_String()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Count_String_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int Count_String_Body()
        {
            return _stringBased.Count;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void Count_Array()
        {
            for (var i = 0; i < Iterations; i++)
            {
                Count_Array_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private int Count_Array_Body()
        {
            return _arrayBased.Count;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void ForEach_String()
        {
            for (var i = 0; i < Iterations; i++)
            {
                ForEach_String_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string ForEach_String_Body()
        {
            var s = "";
            foreach (var item in _stringBased)
            {
                s = item;
            }

            return s;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void ForEach_Array()
        {
            for (var i = 0; i < Iterations; i++)
            {
                ForEach_Array_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string ForEach_Array_Body()
        {
            var s = "";
            foreach (var item in _arrayBased)
            {
                s = item;
            }

            return s;
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void PassByValue()
        {
            for (var i = 0; i < Iterations; i++)
            {
                PassByValue_Body();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string PassByValue_Body()
        {
            return ByValue(_stringBased);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string ByValue(StringValues val) => val.ToString();

#pragma warning disable CS0649
        private struct HeaderReferences
        {
            public StringValues _CacheControl;
            public StringValues _Connection;
            public StringValues _Date;
            public StringValues _KeepAlive;
            public StringValues _Pragma;
            public StringValues _Trailer;
            public StringValues _TransferEncoding;
            public StringValues _Upgrade;
            public StringValues _Via;
            public StringValues _Warning;
            public StringValues _Allow;
            public StringValues _ContentType;
            public StringValues _ContentEncoding;
            public StringValues _ContentLanguage;
            public StringValues _ContentLocation;
            public StringValues _ContentMD5;
            public StringValues _ContentRange;
            public StringValues _Expires;
            public StringValues _LastModified;
            public StringValues _AcceptRanges;
            public StringValues _Age;
            public StringValues _ETag;
            public StringValues _Location;
            public StringValues _ProxyAuthenticate;
            public StringValues _RetryAfter;
            public StringValues _Server;
            public StringValues _SetCookie;
            public StringValues _Vary;
            public StringValues _WWWAuthenticate;
            public StringValues _AccessControlAllowCredentials;
            public StringValues _AccessControlAllowHeaders;
            public StringValues _AccessControlAllowMethods;
            public StringValues _AccessControlAllowOrigin;
            public StringValues _AccessControlExposeHeaders;
            public StringValues _AccessControlMaxAge;

            public byte[] _rawConnection;
            public byte[] _rawDate;
            public byte[] _rawTransferEncoding;
            public byte[] _rawServer;
        }
#pragma warning restore CS0649
    }
}
