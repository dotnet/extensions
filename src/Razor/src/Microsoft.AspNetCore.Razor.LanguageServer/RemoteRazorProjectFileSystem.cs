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

            _root = filePathNormalizer.NormalizeDirectory(root);

            _filePathNormalizer = filePathNormalizer;
        }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            throw new NotImplementedException();
        }

        [Obsolete]
        public override RazorProjectItem GetItem(string path)
        {
            return GetItem(path, fileKind: null);
        }

        public override RazorProjectItem GetItem(string path, string fileKind)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var physicalPath = NormalizeAndEnsureValidPath(path);
            if (FilePathRootedBy(physicalPath, _root))
            {
                var filePath = physicalPath.Substring(_root.Length);
                return new RemoteProjectItem(filePath, physicalPath, fileKind);
            }
            else
            {
                // File does not belong to this file system.
                // In practice this should never happen, the systems above this should have routed the
                // file request to the appropriate file system. Return something reasonable so a higher
                // layer falls over to provide a better error.
                return new RemoteProjectItem(physicalPath, physicalPath, fileKind);
            }
        }

        protected override string NormalizeAndEnsureValidPath(string path)
        {
            var absolutePath = path;
            if (!FilePathRootedBy(absolutePath, _root))
            {
                if (Path.IsPathRooted(absolutePath))
                {
                    // Existing path is already rooted, can't translate from relative to absolute.
                    return absolutePath;
                }

                if (path[0] == '/' || path[0] == '\\')
                {
                    path = path.Substring(1);
                }

                absolutePath = _root + path;
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
