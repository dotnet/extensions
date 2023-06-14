#pragma warning disable IDE0073

// <copyright file="SelfDiagnosticsConfigRefresher.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

// This code was originally copied from the OpenTelemetry-dotnet repo
// https://github.com/open-telemetry/opentelemetry-dotnet/blob/952c3b17fc2eaa0622f5f3efd336d4cf103c2813/src/OpenTelemetry/Internal/SelfDiagnosticsConfigRefresher.cs

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// SelfDiagnosticsConfigRefresher class checks a location for a configuration file
/// and open a MemoryMappedFile of a configured size at the configured file path.
/// The class provides a stream object with proper write position if the configuration
/// file is present and valid. Otherwise, the stream object would be unavailable,
/// nothing will be logged to any file.
/// </summary>
internal class SelfDiagnosticsConfigRefresher : IDisposable
{
    public static readonly byte[] MessageOnNewFile = Encoding.UTF8.GetBytes("Successfully opened file.\n");

    /// <summary>
    /// memoryMappedFileCache is a handle kept in thread-local storage as a cache to indicate whether the cached
    /// viewStream is created from the current m_memoryMappedFile.
    /// </summary>
    internal readonly ThreadLocal<MemoryMappedViewStream?> ViewStream = new(true);

    internal readonly ThreadLocal<MemoryMappedFile> MemoryMappedFileCache = new(true);

    private const int ConfigurationUpdatePeriodMilliSeconds = 10000;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _worker;
    private readonly SelfDiagnosticsConfigParser _configParser;

    private readonly TimeProvider _timeProvider;
    private readonly string _logFileName;

    private bool _disposedValue;

    // Once the configuration file is valid, an eventListener object will be created.
    private SelfDiagnosticsEventListener? _eventListener;
    private volatile FileStream? _underlyingFileStreamForMemoryMappedFile;
    private volatile MemoryMappedFile? _memoryMappedFile;
    private string? _logDirectory;  // Log directory for log files
    private int _logFileSize;  // Log file size in bytes
    private long _logFilePosition;  // The logger will write into the byte at this position
    private EventLevel _logEventLevel = (EventLevel)(-1);

    public SelfDiagnosticsConfigRefresher(
        TimeProvider timeProvider,
        SelfDiagnosticsConfigParser? parser = null,
        string logFileName = "",
        CancellationToken? workerTaskToken = null)
    {
        _timeProvider = timeProvider;

        if (string.IsNullOrEmpty(logFileName))
        {
            _logFileName = GenerateLogFileName();
        }
        else
        {
            _logFileName = logFileName;
        }

        _configParser = parser ?? new SelfDiagnosticsConfigParser();
        UpdateMemoryMappedFileFromConfiguration();
        _cancellationTokenSource = new CancellationTokenSource();
        _worker = Task.Run(() => WorkerAsync(_cancellationTokenSource.Token), workerTaskToken ?? _cancellationTokenSource.Token);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _underlyingFileStreamForMemoryMappedFile?.Dispose();
        _memoryMappedFile?.Dispose();
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Try to get the log stream which is seeked to the position where the next line of log should be written.
    /// </summary>
    /// <param name="byteCount">The number of bytes that need to be written.</param>
    /// <param name="stream">When this method returns, contains the Stream object where `byteCount` of bytes can be written.</param>
    /// <param name="availableByteCount">The number of bytes that is remaining until the end of the stream.</param>
    /// <returns>Whether the logger should log in the stream.</returns>
    public virtual bool TryGetLogStream(int byteCount, out Stream stream, out int availableByteCount)
    {
        if (_memoryMappedFile == null)
        {
            stream = Stream.Null;
            availableByteCount = 0;
            return false;
        }

        try
        {
            var cachedViewStream = ViewStream.Value;

            // Each thread has its own MemoryMappedViewStream created from the only one MemoryMappedFile.
            // Once worker thread updates the MemoryMappedFile, all the cached ViewStream objects become
            // obsolete.
            // Each thread creates a new MemoryMappedViewStream the next time it tries to retrieve it.
            // Whether the MemoryMappedViewStream is obsolete is determined by comparing the current
            // MemoryMappedFile object with the MemoryMappedFile object cached at the creation time of the
            // MemoryMappedViewStream.
            if (cachedViewStream == null || MemoryMappedFileCache.Value != _memoryMappedFile)
            {
                // Race condition: The code might reach here right after the worker thread sets memoryMappedFile
                // to null in CloseLogFile().
                // In this case, let the NullReferenceException be caught and fail silently.
                // By design, all events captured will be dropped during a configuration file refresh if
                // the file changed, regardless whether the file is deleted or updated.
                cachedViewStream = _memoryMappedFile.CreateViewStream();
                ViewStream.Value = cachedViewStream;
                MemoryMappedFileCache.Value = _memoryMappedFile;
            }

            long beginPosition;
            long endPosition;
            do
            {
                beginPosition = _logFilePosition;
                endPosition = beginPosition + byteCount;
                if (endPosition >= _logFileSize)
                {
                    endPosition %= _logFileSize;
                }
            }
            while (beginPosition != Interlocked.CompareExchange(ref _logFilePosition, endPosition, beginPosition));
            availableByteCount = (int)(_logFileSize - beginPosition);
            _ = cachedViewStream.Seek(beginPosition, SeekOrigin.Begin);
            stream = cachedViewStream;
            return true;
        }
#pragma warning disable CA1031 // Do not catch general exception types - this tools is nice-to-have and good if it just works, it should not never throw if anything happens.
        catch (Exception)
        {
            stream = Stream.Null;
            availableByteCount = 0;
            return false;
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    internal async Task WorkerAsync(CancellationToken cancellationToken)
    {
        do
        {
            await _timeProvider.Delay(TimeSpan.FromMilliseconds(ConfigurationUpdatePeriodMilliSeconds), cancellationToken).ConfigureAwait(false);
            UpdateMemoryMappedFileFromConfiguration();
        }
        while (!cancellationToken.IsCancellationRequested);
    }

    [ExcludeFromCodeCoverage]
    private static string GenerateLogFileName()
    {
#if NET5_0_OR_GREATER
        return Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName)
               + ".R9."
               + Environment.ProcessId
               + ".log";
#else
        return Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName)
               + ".R9."
               + Process.GetCurrentProcess().Id
               + ".log";
#endif
    }

    private void UpdateMemoryMappedFileFromConfiguration()
    {
        if (_configParser.TryGetConfiguration(out string? newLogDirectory, out int fileSizeInKb, out EventLevel newEventLevel))
        {
            int newFileSize = fileSizeInKb * 1024;
            if (!newLogDirectory.Equals(_logDirectory, StringComparison.Ordinal) || _logFileSize != newFileSize)
            {
                CloseLogFile();
                OpenLogFile(newLogDirectory, newFileSize);
            }

            if (!newEventLevel.Equals(_logEventLevel))
            {
                _eventListener?.Dispose();
                _eventListener = new SelfDiagnosticsEventListener(newEventLevel, this, _timeProvider);
                _logEventLevel = newEventLevel;
            }
        }
        else
        {
            CloseLogFile();
        }
    }

    private void CloseLogFile()
    {
        MemoryMappedFile? mmf = Interlocked.CompareExchange(ref _memoryMappedFile, null, _memoryMappedFile);
        if (mmf != null)
        {
            // Each thread has its own MemoryMappedViewStream created from the only one MemoryMappedFile.
            // Once worker thread closes the MemoryMappedFile, all the ViewStream objects should be disposed
            // properly.
            foreach (var stream in ViewStream.Values)
            {
                stream?.Dispose();
            }

            mmf.Dispose();
        }

        FileStream? fs = Interlocked.CompareExchange(
            ref _underlyingFileStreamForMemoryMappedFile,
            null,
            _underlyingFileStreamForMemoryMappedFile);
        fs?.Dispose();
    }

    [SuppressMessage("Performance",
        "R9A017:Use asynchronous operations instead of legacy thread blocking code",
        Justification = "Borrowed from OpenTelemetry-dotnet as is.")]
    private void OpenLogFile(string newLogDirectory, int newFileSize)
    {
        try
        {
            _ = Directory.CreateDirectory(newLogDirectory);

            var filePath = Path.Combine(newLogDirectory, _logFileName);

            // Because the API [MemoryMappedFile.CreateFromFile][1](the string version) behaves differently on
            // .NET Framework and .NET Core, here I am using the [FileStream version][2] of it.
            // Taking the last four parameter values from [.NET Framework]
            // (https://referencesource.microsoft.com/#system.core/System/IO/MemoryMappedFiles/MemoryMappedFile.cs,148)
            // and [.NET Core]
            // (https://github.com/dotnet/runtime/blob/master/src/libraries/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedFile.cs#L152)
            // The parameter for FileAccess is different in type but the same in rules, both are Read and Write.
            // The parameter for FileShare is different in values and in behavior.
            // .NET Framework doesn't allow sharing but .NET Core allows reading by other programs.
            // The last two parameters are the same values for both frameworks.
#pragma warning disable S103 // can't split the line because it is a link.

            // [1]: https://docs.microsoft.com/dotnet/api/system.io.memorymappedfiles.memorymappedfile.createfromfile?view=net-5.0#System_IO_MemoryMappedFiles_MemoryMappedFile_CreateFromFile_System_String_System_IO_FileMode_System_String_System_Int64_
            // [2]: https://docs.microsoft.com/dotnet/api/system.io.memorymappedfiles.memorymappedfile.createfromfile?view=net-5.0#System_IO_MemoryMappedFiles_MemoryMappedFile_CreateFromFile_System_IO_FileStream_System_String_System_Int64_System_IO_MemoryMappedFiles_MemoryMappedFileAccess_System_IO_HandleInheritability_System_Boolean_
#pragma warning restore S103
            _underlyingFileStreamForMemoryMappedFile =
                new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 0x1000, FileOptions.None);

            // The parameter values for MemoryMappedFileSecurity, HandleInheritability and leaveOpen are the same
            // values for .NET Framework and .NET Core:
            // https://referencesource.microsoft.com/#system.core/System/IO/MemoryMappedFiles/MemoryMappedFile.cs,172
            // https://github.com/dotnet/runtime/blob/master/src/libraries/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedFile.cs#L168-L179
            _memoryMappedFile = MemoryMappedFile.CreateFromFile(
                _underlyingFileStreamForMemoryMappedFile,
                null,
                newFileSize,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false);
            _logDirectory = newLogDirectory;
            _logFileSize = newFileSize;
            _logFilePosition = MessageOnNewFile.Length;
            using var stream = _memoryMappedFile.CreateViewStream();
            stream.Write(MessageOnNewFile, 0, MessageOnNewFile.Length);
        }
#pragma warning disable CA1031 // Do not catch general exception types - this tools is nice-to-have and good if it just works, it should not never throw if anything happens.
        catch (Exception ex)
        {
            SelfDiagnosticsEventSource.Log.SelfDiagnosticsFileCreateException(newLogDirectory, ex);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            _cancellationTokenSource.Cancel(false);

            try
            {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                _worker.Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            }
            catch (AggregateException)
            {
                // do nothing.
            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }

            // Dispose EventListener before files, because EventListener writes to files.
            _eventListener?.Dispose();

            // Ensure worker thread properly finishes.
            // Or it might have created another MemoryMappedFile in that thread
            // after the CloseLogFile() below is called.
            CloseLogFile();

            // Dispose ThreadLocal variables after the file handles are disposed.
            ViewStream.Dispose();
            MemoryMappedFileCache.Dispose();
        }

        _disposedValue = true;
    }
}
