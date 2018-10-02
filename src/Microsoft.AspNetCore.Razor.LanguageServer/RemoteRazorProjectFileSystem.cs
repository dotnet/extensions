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
        private readonly FilePathNormalizer _filePathNormalizer;

        public RemoteRazorProjectFileSystem(FilePathNormalizer filePathNormalizer)
        {
            if (filePathNormalizer == null)
            {
                throw new ArgumentNullException(nameof(filePathNormalizer));
            }

            _filePathNormalizer = filePathNormalizer;
        }

        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            throw new NotImplementedException();
        }

        public override RazorProjectItem GetItem(string path)
        {
            var physicalFilePath = _filePathNormalizer.Normalize(path);

            return new RemoteProjectItem(path, physicalFilePath);
        }
    }
}
