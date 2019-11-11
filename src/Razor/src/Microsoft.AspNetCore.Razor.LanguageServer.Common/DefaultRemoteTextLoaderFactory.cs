// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    public class DefaultRemoteTextLoaderFactory : RemoteTextLoaderFactory
    {
        private readonly FilePathNormalizer _filePathNormalizer;

        public DefaultRemoteTextLoaderFactory(
            FilePathNormalizer filePathNormalizer)
        {
            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _filePathNormalizer = filePathNormalizer;
        }

        public override TextLoader Create(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var normalizedPath = _filePathNormalizer.Normalize(filePath);
            return new RemoteTextLoader(normalizedPath);
        }

        private class RemoteTextLoader : TextLoader
        {
            private readonly string _filePath;

            public RemoteTextLoader(string filePath)
            {
                if (filePath == null)
                {
                    throw new ArgumentNullException(nameof(filePath));
                }

                _filePath = filePath;
            }

            public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            {
                var physicalFilePath = _filePath;
                if (physicalFilePath[0] == '/')
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // VSLS path, not understood by File.OpenRead so we need to strip the leading separator.
                        physicalFilePath = physicalFilePath.Substring(1);
                    }
                    else
                    {
                        // Unix system, path starts with / which is allowed by File.OpenRead on non-windows.
                    }
                }
                var prevLastWriteTime = File.GetLastWriteTimeUtc(physicalFilePath);

                TextAndVersion textAndVersion;

                using (var stream = File.OpenRead(physicalFilePath))
                {
                    var version = VersionStamp.Create(prevLastWriteTime);
                    var text = SourceText.From(stream);
                    textAndVersion = TextAndVersion.Create(text, version);
                }

                var newLastWriteTime = File.GetLastWriteTimeUtc(physicalFilePath);
                if (!newLastWriteTime.Equals(prevLastWriteTime))
                {
                    throw new IOException($"File was externally modified: {physicalFilePath}");
                }

                return Task.FromResult(textAndVersion);
            }
        }
    }
}
