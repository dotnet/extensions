// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test.Infrastructure
{
    public static class TestRazorSourceDocument
    {
        public static RazorSourceDocument CreateResource(string resourcePath, Type type, Encoding encoding = null, bool normalizeNewLines = false)
        {
            return CreateResource(resourcePath, type.GetTypeInfo().Assembly, encoding, normalizeNewLines);
        }

        public static RazorSourceDocument CreateResource(string resourcePath, Assembly assembly, Encoding encoding = null, bool normalizeNewLines = false)
        {
            var file = TestFile.Create(resourcePath, assembly);

            using (var input = file.OpenRead())
            using (var reader = new StreamReader(input))
            {
                var content = reader.ReadToEnd();
                if (normalizeNewLines)
                {
                    content = NormalizeNewLines(content);
                }

                var properties = new RazorSourceDocumentProperties(resourcePath, resourcePath);
                return new StringSourceDocument(content, encoding ?? Encoding.UTF8, properties);
            }
        }

        public static RazorSourceDocument CreateResource(
            string path,
            Assembly assembly,
            Encoding encoding,
            RazorSourceDocumentProperties properties,
            bool normalizeNewLines = false)
        {
            var file = TestFile.Create(path, assembly);

            using (var input = file.OpenRead())
            using (var reader = new StreamReader(input))
            {
                var content = reader.ReadToEnd();
                if (normalizeNewLines)
                {
                    content = NormalizeNewLines(content);
                }

                return new StringSourceDocument(content, encoding ?? Encoding.UTF8, properties);
            }
        }

        public static MemoryStream CreateStreamContent(string content = "Hello, World!", Encoding encoding = null, bool normalizeNewLines = false)
        {
            var stream = new MemoryStream();
            encoding = encoding ?? Encoding.UTF8;
            using (var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true))
            {
                if (normalizeNewLines)
                {
                    content = NormalizeNewLines(content);
                }

                writer.Write(content);
            }

            stream.Seek(0L, SeekOrigin.Begin);

            return stream;
        }

        public static RazorSourceDocument Create(
            string content = "Hello, world!",
            Encoding encoding = null,
            bool normalizeNewLines = false,
            string filePath = "test.cshtml",
            string relativePath = "test.cshtml")
        {
            if (normalizeNewLines)
            {
                content = NormalizeNewLines(content);
            }

            var properties = new RazorSourceDocumentProperties(filePath, relativePath);
            return new StringSourceDocument(content, encoding ?? Encoding.UTF8, properties);
        }

        public static RazorSourceDocument Create(
            string content,
            RazorSourceDocumentProperties properties,
            Encoding encoding = null,
            bool normalizeNewLines = false)
        {
            if (normalizeNewLines)
            {
                content = NormalizeNewLines(content);
            }

            return new StringSourceDocument(content, encoding ?? Encoding.UTF8, properties);
        }

        private static string NormalizeNewLines(string content)
        {
            return Regex.Replace(content, "(?<!\r)\n", "\r\n", RegexOptions.None, TimeSpan.FromSeconds(10));
        }


        private class TestFile
        {
            private TestFile(string resourceName, Assembly assembly)
            {
                Assembly = assembly;
                ResourceName = Assembly.GetName().Name + "." + resourceName.Replace('/', '.');
            }

            public Assembly Assembly { get; }

            public string ResourceName { get; }

            public static TestFile Create(string resourceName, Type type)
            {
                return new TestFile(resourceName, type.GetTypeInfo().Assembly);
            }

            public static TestFile Create(string resourceName, Assembly assembly)
            {
                return new TestFile(resourceName, assembly);
            }

            public Stream OpenRead()
            {
                var stream = Assembly.GetManifestResourceStream(ResourceName);
                if (stream == null)
                {
                    Assert.True(false, string.Format("Manifest resource: {0} not found", ResourceName));
                }

                return stream;
            }

            public bool Exists()
            {
                var resourceNames = Assembly.GetManifestResourceNames();
                foreach (var resourceName in resourceNames)
                {
                    // Resource names are case-sensitive.
                    if (string.Equals(ResourceName, resourceName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }

            public string ReadAllText()
            {
                using (var reader = new StreamReader(OpenRead()))
                {
                    // The .Replace() calls normalize line endings, in case you get \n instead of \r\n
                    // since all the unit tests rely on the assumption that the files will have \r\n endings.
                    return reader.ReadToEnd().Replace("\r", "").Replace("\n", "\r\n");
                }
            }

            /// <summary>
            /// Saves the file to the specified path.
            /// </summary>
            public void Save(string filePath)
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var outStream = File.Create(filePath))
                {
                    using (var inStream = OpenRead())
                    {
                        inStream.CopyTo(outStream);
                    }
                }
            }
        }
    }
}
