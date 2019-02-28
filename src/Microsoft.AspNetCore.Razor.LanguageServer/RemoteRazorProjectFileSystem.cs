// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RemoteRazorProjectFileSystem : RazorProjectFileSystem
    {
        private readonly string _root;
        private readonly FilePathNormalizer _filePathNormalizer;

        public RemoteRazorProjectFileSystem(
            string root,
            FilePathNormalizer filePathNormalizer)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _root = filePathNormalizer.Normalize(root);
            _filePathNormalizer = filePathNormalizer;
        }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            throw new NotImplementedException();
        }

        public override RazorProjectItem GetItem(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var physicalPath = NormalizeAndEnsureValidPath(path);
            if (FilePathRootedBy(physicalPath, _root))
            {
                var filePath = physicalPath.Substring(_root.Length + 1 /* / */);
                return new RemoteProjectItem(filePath, physicalPath);
            }
            else
            {
                // File does not belong to this file system.
                // In practice this should never happen, the systems above this should have routed the
                // file request to the appropriate file system. Return something reasonable so a higher
                // layer falls over to provide a better error.
                return new RemoteProjectItem(physicalPath, physicalPath);
            }
        }

        protected override string NormalizeAndEnsureValidPath(string path)
        {
            var absolutePath = path;
            if (!FilePathRootedBy(absolutePath, _root))
            {
                if (path[0] == '/' || path[0] == '\\')
                {
                    path = path.Substring(1);
                }

                absolutePath = Path.Combine(_root, path);
            }

            absolutePath = _filePathNormalizer.Normalize(absolutePath);
            return absolutePath;
        }

        internal bool FilePathRootedBy(string path, string root)
        {
            if (path.Length < root.Length)
            {
                return false;
            }

            var potentialRoot = path.Substring(0, root.Length);
            if (FilePathComparer.Instance.Equals(potentialRoot, root))
            {
                return true;
            }

            return false;
        }
    }
}
