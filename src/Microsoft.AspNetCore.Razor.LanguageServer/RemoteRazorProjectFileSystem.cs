// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class RemoteRazorProjectFileSystem : RazorProjectFileSystem
    {
        private readonly RazorProjectFileSystem _inner;
        private readonly FilePathNormalizer _filePathNormalizer;

        public RemoteRazorProjectFileSystem(RazorProjectFileSystem inner, FilePathNormalizer filePathNormalizer)
        {
            if (inner == null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _inner = inner;
            _filePathNormalizer = filePathNormalizer;
        }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            var innerItems = _inner.EnumerateItems(basePath);
            var normalizedItems = innerItems.Select(NormalizeItem);

            return normalizedItems;
        }

        public override RazorProjectItem GetItem(string path)
        {
            var innerItem = _inner.GetItem(path);
            var normalizedItem = NormalizeItem(innerItem);

            return normalizedItem;
        }

        private RazorProjectItem NormalizeItem(RazorProjectItem item)
        {
            var normalizedPhysicalPath = _filePathNormalizer.Normalize(item.PhysicalPath);
            return new RemoteProjectItem(item, normalizedPhysicalPath);
        }
    }
}
