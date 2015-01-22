// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.FileSystemGlobbing.Abstractions;

namespace Microsoft.Framework.FileSystemGlobbing.Infrastructure
{
    public class MatcherContext
    {
        public MatcherContext(Matcher matcher, DirectoryInfoBase directoryInfo)
        {
            Matcher = matcher;
            DirectoryInfo = directoryInfo;
            foreach (var pattern in matcher.IncludePatterns)
            {
                if (pattern.Contains == null)
                {
                    IncludePatternContexts.Add(new PatternContextLinearInclude(this, pattern));
                }
                else
                {
                    IncludePatternContexts.Add(new PatternContextRaggedInclude(this, pattern));
                }
            }
            foreach (var pattern in matcher.ExcludePatterns)
            {
                if (pattern.Contains == null)
                {
                    ExcludePatternContexts.Add(new PatternContextLinearExclude(this, pattern));
                }
                else
                {
                    ExcludePatternContexts.Add(new PatternContextRaggedExclude(this, pattern));
                }
            }
        }

        public Matcher Matcher { get; }
        public DirectoryInfoBase DirectoryInfo { get; }
        public IList<PatternContextBase> IncludePatternContexts { get; } = new List<PatternContextBase>();
        public IList<PatternContextBase> ExcludePatternContexts { get; } = new List<PatternContextBase>();
        public List<string> Files { get; private set; }

        public FrameData Frame;
        public Stack<FrameData> FrameStack = new Stack<FrameData>();

        public PatternMatchingResult Execute()
        {
            Files = new List<string>();

            Frame.Stage = Stage.Complete;
            PushFrame(Stage.Predicting, DirectoryInfo);

            while (Frame.Stage != Stage.Complete)
            {
                DoPredicting();
                DoEnumerating();
                DoRecursion();
            }

            return new PatternMatchingResult(Files);
        }

        private void DoPredicting()
        {
            if (Frame.Stage != Stage.Predicting)
            {
                return;
            }
            //foreach (var patternContext in IncludePatternContexts)
            //{
            //    patternContext.PredictInclude();
            //}
            //foreach (var patternContext in ExcludePatternContexts)
            //{
            //    //patternContext.PredictExclude();
            //}
            Frame.Stage = Stage.Enumerating;
        }

        private void DoEnumerating()
        {
            if (Frame.Stage != Stage.Enumerating)
            {
                return;
            }
            foreach (var fileSystemInfo in Frame.DirectoryInfo.EnumerateFileSystemInfos(
                "*",
                SearchOption.TopDirectoryOnly))
            {
                var directoryInfo = fileSystemInfo as DirectoryInfoBase;
                if (directoryInfo != null)
                {
                    var include = false;
                    foreach (var pattern in IncludePatternContexts)
                    {
                        if (pattern.Test(directoryInfo))
                        {
                            include = true;
                            break;
                        }
                    }
                    if (include)
                    {
                        foreach (var pattern in ExcludePatternContexts)
                        {
                            if (pattern.Test(directoryInfo))
                            {
                                include = false;
                                break;
                            }
                        }
                    }
                    if (include)
                    {
                        if (Frame.ActualDirectories == null)
                        {
                            Frame.ActualDirectories = new List<DirectoryInfoBase>();
                        }
                        Frame.ActualDirectories.Add(directoryInfo);
                    }
                    continue;
                }
                var fileInfo = fileSystemInfo as FileInfoBase;
                if (fileInfo != null)
                {
                    var include = false;
                    foreach (var pattern in IncludePatternContexts)
                    {
                        if (pattern.Test(fileInfo))
                        {
                            include = true;
                            break;
                        }
                    }
                    if (include)
                    {
                        foreach (var pattern in ExcludePatternContexts)
                        {
                            if (pattern.Test(fileInfo))
                            {
                                include = false;
                                break;
                            }
                        }
                    }
                    if (include)
                    {
                        if (Frame.RelativePath == null)
                        {
                            Files.Add(fileInfo.Name);
                        }
                        else
                        {
                            Files.Add(Frame.RelativePath + "/" + fileInfo.Name);
                        }
                    }
                    continue;
                }
            }
            Frame.Stage = Stage.Recursing;
        }

        private void DoRecursion()
        {
            if (Frame.Stage != Stage.Recursing)
            {
                return;
            }

            if (Frame.ActualDirectories == null)
            {
                PopFrame();
                return;
            }

            if (Frame.ActualDirectoryEnumerating == false)
            {
                Frame.ActualDirectoryEnumerating = true;
                Frame.ActualDirectoryEnumerator = Frame.ActualDirectories.GetEnumerator();
            }

            var moveNext = Frame.ActualDirectoryEnumerator.MoveNext();
            if (moveNext == false)
            {
                PopFrame();
                return;
            }

            PushFrame(Stage.Predicting, Frame.ActualDirectoryEnumerator.Current);
        }


        public void AddPredictIncludeLiteral(string value)
        {
            if (Frame.PredictLiteralIncludes == null)
            {
                Frame.PredictLiteralIncludes = new List<string>();
            }
            //TODO: string comparisons
            if (!Frame.PredictLiteralIncludes.Contains(value))
            {
                Frame.PredictLiteralIncludes.Add(value);
            }
        }

        public void PushFrame(Stage stage, DirectoryInfoBase directoryInfo)
        {
            string relativePath;
            if (FrameStack.Count == 0)
            {
                relativePath = null;
            }
            else if (FrameStack.Count == 1)
            {
                relativePath = directoryInfo.Name;
            }
            else
            {
                relativePath = Frame.RelativePath + '/' + directoryInfo.Name;
            }

            FrameStack.Push(Frame);
            Frame = new FrameData
            {
                Stage = stage,
                DirectoryInfo = directoryInfo,
                RelativePath = relativePath,
            };
            foreach (var x in IncludePatternContexts)
            {
                x.PushFrame(directoryInfo);
            }
            foreach (var x in ExcludePatternContexts)
            {
                x.PushFrame(directoryInfo);
            }
        }

        public void PopFrame()
        {
            foreach (var x in ExcludePatternContexts)
            {
                x.PopFrame();
            }
            foreach (var x in IncludePatternContexts)
            {
                x.PopFrame();
            }
            Frame = FrameStack.Pop();
        }

        public enum Stage
        {
            Initialized,
            Predicting,
            Enumerating,
            Recursing,
            Complete,
        }

        public struct FrameData
        {
            public Stage Stage;
            public DirectoryInfoBase DirectoryInfo;
            public string RelativePath;

            public List<string> PredictLiteralIncludes;

            public List<DirectoryInfoBase> ActualDirectories;
            public bool ActualDirectoryEnumerating;
            public List<DirectoryInfoBase>.Enumerator ActualDirectoryEnumerator;
        }
    }
}
