// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.TestUtility
{
    internal class MockFileInfo : FileInfoBase
    {
        public MockFileInfo(
            FileSystemOperationRecorder recorder,
            DirectoryInfoBase parentDirectory,
            string fullName,
            string name)
        {
            Recorder = recorder;
            FullName = fullName;
            Name = name;
        }

        public FileSystemOperationRecorder Recorder { get; }

        public override DirectoryInfoBase ParentDirectory { get; }

        public override string FullName { get; }

        public override string Name { get; }
    }
}