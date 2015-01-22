// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;

namespace Microsoft.Framework.FileSystemGlobbing.Abstractions
{
    public class DirectoryInfoWrapper : DirectoryInfoBase
    {
        private readonly DirectoryInfo _directoryInfo;

        public DirectoryInfoWrapper(DirectoryInfo directoryInfo)
        {
            _directoryInfo = directoryInfo;
        }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            foreach (var fileSystemInfo in _directoryInfo.EnumerateFileSystemInfos(searchPattern, searchOption))
            {
                var directoryInfo = fileSystemInfo as DirectoryInfo;
                if (directoryInfo != null)
                {
                    yield return new DirectoryInfoWrapper(directoryInfo);
                }
                else
                {
                    yield return new FileInfoWrapper((FileInfo)fileSystemInfo);
                }
            }
        }

        public override string Name
        {
            get { return _directoryInfo.Name; }
        }

        public override string FullName
        {
            get { return _directoryInfo.FullName; }
        }

        public override DirectoryInfoBase ParentDirectory
        {
            get { return new DirectoryInfoWrapper(_directoryInfo.Parent); }
        }
    }
}
