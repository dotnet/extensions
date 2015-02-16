// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;
using Microsoft.Framework.FileSystemGlobbing.Internal.PathSegments;

namespace Microsoft.Framework.FileSystemGlobbing.Internal
{
    public class MatcherContext
    {
        private readonly DirectoryInfoBase _root;
        private readonly IList<IPatternContext> _includePatternContexts;
        private readonly IList<IPatternContext> _excludePatternContexts;
        private readonly IList<string> _files;

        private readonly IList<LiteralPathSegment> _declaredLiteralFolderSegments = new List<LiteralPathSegment>();
        private readonly IList<LiteralPathSegment> _declaredLiteralFileSegments = new List<LiteralPathSegment>();

        private bool _declaredParentPathSegment;
        private bool _declaredWildcardPathSegment;

        public MatcherContext(IEnumerable<IPattern> includePatterns, IEnumerable<IPattern> excludePatterns, DirectoryInfoBase directoryInfo)
        {
            _root = directoryInfo;
            _files = new List<string>();

            _includePatternContexts = includePatterns.Select(pattern => pattern.CreatePatternContextForInclude()).ToList();
            _excludePatternContexts = excludePatterns.Select(pattern => pattern.CreatePatternContextForExclude()).ToList();
        }

        public PatternMatchingResult Execute()
        {
            _files.Clear();

            Match(_root, parentRelativePath: null);

            return new PatternMatchingResult(_files);
        }

        private void Match(DirectoryInfoBase directory, string parentRelativePath)
        {
            // Request all the including and excluding patterns to push current directory onto their status stack.
            PushDirectory(directory);
            Declare();

            IEnumerable<FileSystemInfoBase> entities = null;
            if (_declaredWildcardPathSegment || _declaredLiteralFileSegments.Any())
            {
                entities = directory.EnumerateFileSystemInfos();
            }
            else
            {
                entities = _declaredLiteralFolderSegments.Select(literal => directory.GetDirectory(literal.Value));
            }

            if (_declaredParentPathSegment)
            {
                entities = entities.Concat(new[] { directory.GetDirectory("..") });
            }

            // collect files and sub directories
            var subDirectories = new List<DirectoryInfoBase>();
            foreach (var entity in entities)
            {
                var fileInfo = entity as FileInfoBase;
                if (fileInfo != null)
                {
                    if (MatchPatternContexts(fileInfo, (pattern, file) => pattern.Test(file)))
                    {
                        _files.Add(CombinePath(parentRelativePath, fileInfo.Name));
                    }

                    continue;
                }

                var directoryInfo = entity as DirectoryInfoBase;
                if (directoryInfo != null)
                {
                    if (MatchPatternContexts(directoryInfo, (pattern, dir) => pattern.Test(dir)))
                    {
                        subDirectories.Add(directoryInfo);
                    }

                    continue;
                }
            }

            // Matches the sub directories recursively
            foreach (var subDir in subDirectories)
            {
                var relativePath = CombinePath(parentRelativePath, subDir.Name);

                Match(subDir, relativePath);
            }

            // Request all the including and excluding patterns to pop their status stack.
            PopDirectory();
        }

        private void Declare()
        {
            _declaredLiteralFileSegments.Clear();
            _declaredLiteralFolderSegments.Clear();
            _declaredParentPathSegment = false;
            _declaredWildcardPathSegment = false;

            foreach (var include in _includePatternContexts)
            {
                include.Declare(DeclareInclude);
            }
        }

        private void DeclareInclude(IPathSegment patternSegment, bool isLastSegment)
        {
            var literalSegment = patternSegment as LiteralPathSegment;
            if (literalSegment != null)
            {
                if (isLastSegment)
                {
                    _declaredLiteralFileSegments.Add(literalSegment);
                }
                else
                {
                    _declaredLiteralFolderSegments.Add(literalSegment);
                }
            }
            else if (patternSegment is ParentPathSegment)
            {
                _declaredParentPathSegment = true;
            }
            else if (patternSegment is WildcardPathSegment)
            {
                _declaredWildcardPathSegment = true;
            }
        }

        private string CombinePath(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }
            else
            {
                return string.Format("{0}/{1}", left, right);
            }
        }

        private bool MatchPatternContexts<TFileInfoBase>(TFileInfoBase fileinfo, Func<IPatternContext, TFileInfoBase, bool> test)
        {
            var found = false;

            // If the given file/directory matches any including pattern, continues to next step.
            foreach (var context in _includePatternContexts)
            {
                if (test(context, fileinfo))
                {
                    found = true;
                    break;
                }
            }

            // If the given file/directory doesn't match any of the including pattern, returns false.
            if (!found)
            {
                return false;
            }

            // If the given file/directory matches any excluding pattern, returns false.
            foreach (var context in _excludePatternContexts)
            {
                if (test(context, fileinfo))
                {
                    return false;
                }
            }

            return true;
        }

        private void PopDirectory()
        {
            foreach (var context in _excludePatternContexts)
            {
                context.PopDirectory();
            }

            foreach (var context in _includePatternContexts)
            {
                context.PopDirectory();
            }
        }

        private void PushDirectory(DirectoryInfoBase directory)
        {
            foreach (var context in _includePatternContexts)
            {
                context.PushDirectory(directory);
            }

            foreach (var context in _excludePatternContexts)
            {
                context.PushDirectory(directory);
            }
        }
    }
}
