// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
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

                TextAndVersion textAndVersion;

                try
                {
                    var prevLastWriteTime = File.GetLastWriteTimeUtc(_filePath);

                    using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {
                        var version = VersionStamp.Create(prevLastWriteTime);
                        var text = SourceText.From(stream);
                        textAndVersion = TextAndVersion.Create(text, version);
                    }

                    var newLastWriteTime = File.GetLastWriteTimeUtc(_filePath);
                    if (!newLastWriteTime.Equals(prevLastWriteTime))
                    {
                        throw new IOException($"File was externally modified: {_filePath}");
                    }
                }
                catch (IOException e) when (e is DirectoryNotFoundException || e is FileNotFoundException)
                {
                    // This can typically occur when a file is renamed. What happens is the client "closes" the old file before any file system "rename" event makes it to us. Resulting
                    // in us trying to refresh the "closed" files buffer with what's on disk; however, there's nothing actually on disk because the file was renamed.
                    textAndVersion = TextAndVersion.Create(SourceText.From(string.Empty), VersionStamp.Default, filePath: _filePath);
                }

                return Task.FromResult(textAndVersion);
            }
        }
    }
}
