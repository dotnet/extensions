// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.WebEncoders.Testing
{
    /// <summary>
    /// Dummy no-op encoder used for unit testing.
    /// </summary>
    public sealed class NullTestEncoder : IHtmlEncoder, IJavaScriptStringEncoder, IUrlEncoder
    {
        public string HtmlEncode(string value)
        {
            return EncodeCore(value);
        }

        public void HtmlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        public void HtmlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        public string JavaScriptStringEncode(string value)
        {
            return EncodeCore(value);
        }

        public void JavaScriptStringEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        public void JavaScriptStringEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        public string UrlEncode(string value)
        {
            return EncodeCore(value);
        }

        public void UrlEncode(string value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        public void UrlEncode(char[] value, int startIndex, int charCount, TextWriter output)
        {
            EncodeCore(value, startIndex, charCount, output);
        }

        private static string EncodeCore(string value)
        {
            return value;
        }

        private static void EncodeCore(string value, int startIndex, int charCount, TextWriter output)
        {
            output.Write(EncodeCore(value.Substring(startIndex, charCount)));
        }

        private static void EncodeCore(char[] value, int startIndex, int charCount, TextWriter output)
        {
            output.Write(EncodeCore(new string(value, startIndex, charCount)));
        }
    }
}