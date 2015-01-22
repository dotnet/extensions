// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.Infrastructure
{
    internal class DirectoryInfoStub : DirectoryInfoBase
    {
        public DirectoryInfoStub(
            SystemIoRecorder recorder,
            DirectoryInfoBase parentDirectory,
            string fullName,
            string name,
            string[] paths)
        {
            ParentDirectory = parentDirectory;
            Recorder = recorder;
            FullName = fullName;
            Name = name;
            Paths = paths;
        }

        public SystemIoRecorder Recorder { get; }

        public override string FullName { get; }

        public override string Name { get; }

        public override DirectoryInfoBase ParentDirectory { get; }

        public string[] Paths { get; }

        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            Recorder.Add("EnumerateFileSystemInfos", new { FullName, Name, searchPattern, searchOption });

            var names = new HashSet<string>();

            foreach (var path in Paths)
            {
                if (!path.Replace('\\', '/').StartsWith(FullName.Replace('\\', '/')))
                {
                    continue;
                }
                var beginPath = FullName.Length;
                var endPath = path.Length;

                var beginSegment = beginPath;
                var endSegment = NextIndex(path, new[] { '/', '\\' }, beginSegment, path.Length);

                if (endPath == endSegment)
                {
                    yield return new FileInfoStub(
                        recorder: Recorder,
                        parentDirectory: this,
                        fullName: path,
                        name: path.Substring(beginSegment, endSegment - beginSegment));
                }
                else
                {
                    var name = path.Substring(beginSegment, endSegment - beginSegment);
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                        yield return new DirectoryInfoStub(
                            recorder: Recorder,
                            parentDirectory: this,
                            fullName: path.Substring(0, endSegment + 1),
                            name: name,
                            paths: Paths);
                    }
                }
            }
        }

        private int NextIndex(string pattern, char[] anyOf, int startIndex, int endIndex)
        {
            var index = pattern.IndexOfAny(anyOf, startIndex, endIndex - startIndex);
            return index == -1 ? endIndex : index;
        }
    }
}
