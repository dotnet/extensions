// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Framework.FileSystemGlobbing.Abstractions
{
    public class FileInfoWrapper : FileInfoBase
    {
        private FileInfo FileInfo;

        public FileInfoWrapper(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }

        public override string Name
        {
            get { return FileInfo.Name; }
        }

        public override string FullName
        {
            get { return FileInfo.FullName; }
        }

        public override DirectoryInfoBase ParentDirectory
        {
            get { return new DirectoryInfoWrapper(FileInfo.Directory); }
        }
    }
}