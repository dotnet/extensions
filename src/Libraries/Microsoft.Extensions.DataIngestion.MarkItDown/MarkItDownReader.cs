// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

public class MarkItDownReader : IngestionDocumentReader
{
    private readonly string _exePath;
    private readonly bool _extractImages;
    private readonly MarkdownReader _markdownReader;

    public MarkItDownReader(string exePath = "markitdown", bool extractImages = false)
    {
        _exePath = exePath ?? throw new ArgumentNullException(nameof(exePath));
        _extractImages = extractImages;
        _markdownReader = new();
    }

    public override async Task<IngestionDocument> ReadAsync(FileInfo source, string identifier, string? mediaType = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        else if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentNullException(nameof(identifier));
        }
        else if (!source.Exists)
        {
            throw new FileNotFoundException("The specified file does not exist.", source.FullName);
        }

        ProcessStartInfo startInfo = new()
        {
            FileName = _exePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        // Force UTF-8 encoding in the environment (will produce garbage otherwise).
        startInfo.Environment["PYTHONIOENCODING"] = "utf-8";
        startInfo.Environment["LC_ALL"] = "C.UTF-8";
        startInfo.Environment["LANG"] = "C.UTF-8";

#if NET
        startInfo.ArgumentList.Add(source.FullName);
        if (_extractImages)
        {
            startInfo.ArgumentList.Add("--keep-data-uris");
        }
#else
        startInfo.Arguments = $"\"{source.FullName}\"" + (_extractImages ? " --keep-data-uris" : "");
#endif

        string outputContent = "";
        using (Process process = new() { StartInfo = startInfo })
        {
            process.Start();

            // Read standard output asynchronously
            outputContent = await process.StandardOutput.ReadToEndAsync(
#if NET
                cancellationToken
#endif
            );

#if NET
            await process.WaitForExitAsync(cancellationToken);
#else
            process.WaitForExit();
#endif

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"MarkItDown process failed with exit code {process.ExitCode}.");
            }
        }

        return _markdownReader.Read(outputContent, identifier);
    }

    public override async Task<IngestionDocument> ReadAsync(Stream source, string identifier, string mediaType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        else if (string.IsNullOrEmpty(identifier))
        {
            throw new ArgumentNullException(nameof(identifier));
        }

        // Instead of creating a temporary file, we could write to the StandardInput of the process.
        // MarkItDown says it supports reading from stdin, but it does not work as expected.
        // Even the sample command line does not work with stdin: "cat example.pdf | markitdown"
        // I can be doing something wrong, but for now, let's write to a temporary file.
        string inputFilePath = Path.GetTempFileName();
        using (FileStream inputFile = new(inputFilePath, FileMode.Open, FileAccess.Write, FileShare.None, bufferSize: 1, FileOptions.Asynchronous))
        {
            await source.CopyToAsync(inputFile
#if NET
                , cancellationToken
#endif
            );
        }

        try
        {
            return await ReadAsync(new FileInfo(inputFilePath), identifier, mediaType, cancellationToken);
        }
        finally
        {
            File.Delete(inputFilePath);
        }
    }
}
