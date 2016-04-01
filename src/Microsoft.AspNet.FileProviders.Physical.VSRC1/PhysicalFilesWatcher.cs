// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Primitives.VSRC1;

namespace Microsoft.AspNet.FileProviders.VSRC1
{
    internal class PhysicalFilesWatcher
    {
        private readonly ConcurrentDictionary<string, FileChangeToken> _tokenCache =
            new ConcurrentDictionary<string, FileChangeToken>(StringComparer.OrdinalIgnoreCase);

        private readonly FileSystemWatcher _fileWatcher;

        private readonly object _lockObject = new object();

        private readonly string _root;

        internal PhysicalFilesWatcher(string root)
        {
            _root = root;
            _fileWatcher = new FileSystemWatcher(root);
            _fileWatcher.IncludeSubdirectories = true;
            _fileWatcher.Created += OnChanged;
            _fileWatcher.Changed += OnChanged;
            _fileWatcher.Renamed += OnRenamed;
            _fileWatcher.Deleted += OnChanged;
            _fileWatcher.Error += OnError;
        }

        internal IChangeToken CreateFileChangeToken(string filter)
        {
            filter = NormalizeFilter(filter);
            var pattern = WildcardToRegexPattern(filter);

            FileChangeToken changeToken;
            if (!_tokenCache.TryGetValue(pattern, out changeToken))
            {
                changeToken = _tokenCache.GetOrAdd(pattern, new FileChangeToken(pattern));
                lock (_lockObject)
                {
                    if (_tokenCache.Count > 0 && !_fileWatcher.EnableRaisingEvents)
                    {
                        // Perf: Turn on the file monitoring if there is something to monitor.
                        _fileWatcher.EnableRaisingEvents = true;
                    }
                }
            }

            return changeToken;
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            // For a file name change or a directory's name change notify registered tokens.
            OnFileSystemEntryChange(e.OldFullPath);
            OnFileSystemEntryChange(e.FullPath);

            if (Directory.Exists(e.FullPath))
            {
                // If the renamed entity is a directory then notify tokens for every sub item.
                foreach (var newLocation in Directory.EnumerateFileSystemEntries(e.FullPath, "*", SearchOption.AllDirectories))
                {
                    // Calculated previous path of this moved item.
                    var oldLocation = Path.Combine(e.OldFullPath, newLocation.Substring(e.FullPath.Length + 1));
                    OnFileSystemEntryChange(oldLocation);
                    OnFileSystemEntryChange(newLocation);
                }
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            OnFileSystemEntryChange(e.FullPath);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            // Notify all cache entries on error.
            foreach (var token in _tokenCache.Values)
            {
                ReportChangeForMatchedEntries(token.Pattern);
            }
        }

        private void OnFileSystemEntryChange(string fullPath)
        {
            var fileSystemInfo = new FileInfo(fullPath);
            if (FileSystemInfoHelper.IsHiddenFile(fileSystemInfo))
            {
                return;
            }

            var relativePath = fullPath.Substring(_root.Length);
            if (_tokenCache.ContainsKey(relativePath))
            {
                ReportChangeForMatchedEntries(relativePath);
            }
            else
            {
                foreach (var token in _tokenCache.Values.Where(t => t.IsMatch(relativePath)))
                {
                    ReportChangeForMatchedEntries(token.Pattern);
                }
            }
        }

        private void ReportChangeForMatchedEntries(string pattern)
        {
            FileChangeToken changeToken;
            if (_tokenCache.TryRemove(pattern, out changeToken))
            {
                changeToken.Changed();
                if (_tokenCache.Count == 0)
                {
                    lock (_lockObject)
                    {
                        if (_tokenCache.Count == 0 && _fileWatcher.EnableRaisingEvents)
                        {
                            // Perf: Turn off the file monitoring if no files to monitor.
                            _fileWatcher.EnableRaisingEvents = false;
                        }
                    }
                }
            }
        }

        private string NormalizeFilter(string filter)
        {
            // If the searchPath ends with \ or /, we treat searchPath as a directory,
            // and will include everything under it, recursively.
            if (IsDirectoryPath(filter))
            {
                filter = filter + "**" + Path.DirectorySeparatorChar + "*";
            }

            filter = Path.DirectorySeparatorChar == '/' ?
                filter.Replace('\\', Path.DirectorySeparatorChar) :
                filter.Replace('/', Path.DirectorySeparatorChar);

            return filter;
        }

        private bool IsDirectoryPath(string path)
        {
            return path != null && path.Length >= 1 && (path[path.Length - 1] == Path.DirectorySeparatorChar || path[path.Length - 1] == Path.AltDirectorySeparatorChar);
        }

        private string WildcardToRegexPattern(string wildcard)
        {
            var regex = Regex.Escape(wildcard);

            if (Path.DirectorySeparatorChar == '/')
            {
                // regex wildcard adjustments for *nix-style file systems.
                regex = regex
                    .Replace(@"\*\*/", "(.*/)?") //For recursive wildcards /**/, include the current directory.
                    .Replace(@"\*\*", ".*") // For recursive wildcards that don't end in a slash e.g. **.txt would be treated as a .txt file at any depth
                    .Replace(@"\*\.\*", @"\*") // "*.*" is equivalent to "*"
                    .Replace(@"\*", @"[^/]*(/)?") // For non recursive searches, limit it any character that is not a directory separator
                    .Replace(@"\?", "."); // ? translates to a single any character
            }
            else
            {
                // regex wildcard adjustments for Windows-style file systems.
                regex = regex
                    .Replace("/", @"\\") // On Windows, / is treated the same as \.
                    .Replace(@"\*\*\\", @"(.*\\)?") //For recursive wildcards \**\, include the current directory.
                    .Replace(@"\*\*", ".*") // For recursive wildcards that don't end in a slash e.g. **.txt would be treated as a .txt file at any depth
                    .Replace(@"\*\.\*", @"\*") // "*.*" is equivalent to "*"
                    .Replace(@"\*", @"[^\\]*(\\)?") // For non recursive searches, limit it any character that is not a directory separator
                    .Replace(@"\?", "."); // ? translates to a single any character
            }

            return regex;
        }
    }
}