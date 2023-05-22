// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

/// <summary>
/// Copied from https://github.com/open-telemetry/opentelemetry-dotnet/blob/952c3b17fc2eaa0622f5f3efd336d4cf103c2813/test/OpenTelemetry.Tests/Internal/SelfDiagnosticsConfigParserTest.cs.
/// </summary>
public class SelfDiagnosticsConfigParserTest
{
    [Fact]
    public void SelfDiagnosticsConfigParser_TryParseFilePath_Success()
    {
        string configJson = "{ \t \n "
                            + "\t    \"LogDirectory\" \t : \"Diagnostics\", \n"
                            + "FileSize \t : \t \n"
                            + " 1024 \n}\n";
        Assert.True(SelfDiagnosticsConfigParser.TryParseLogDirectory(configJson, out string logDirectory));
        Assert.Equal("Diagnostics", logDirectory);
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryParseFilePath_MissingField()
    {
        string configJson = @"{
                ""path"": ""Diagnostics"",
                ""FileSize"": 1024
                }";
        Assert.False(SelfDiagnosticsConfigParser.TryParseLogDirectory(configJson, out _));
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryParseFileSize()
    {
        string configJson = @"{
                ""LogDirectory"": ""Diagnostics"",
                ""FileSize"": 1024
                }";
        Assert.True(SelfDiagnosticsConfigParser.TryParseFileSize(configJson, out int fileSize));
        Assert.Equal(1024, fileSize);
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryParseFileSize_CaseInsensitive()
    {
        string configJson = @"{
                ""LogDirectory"": ""Diagnostics"",
                ""fileSize"" :
                               2048
                }";
        Assert.True(SelfDiagnosticsConfigParser.TryParseFileSize(configJson, out int fileSize));
        Assert.Equal(2048, fileSize);
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryParseFileSize_MissingField()
    {
        string configJson = @"{
                ""LogDirectory"": ""Diagnostics"",
                ""size"": 1024
                }";
        Assert.False(SelfDiagnosticsConfigParser.TryParseFileSize(configJson, out _));
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryParseLogLevel()
    {
        string configJson = @"{
                ""LogDirectory"": ""Diagnostics"",
                ""FileSize"": 1024,
                ""LogLevel"": ""Error""
                }";
        Assert.True(SelfDiagnosticsConfigParser.TryParseLogLevel(configJson, out string logLevelString));
        Assert.Equal("Error", logLevelString);
    }

    [Theory]
    [InlineData(@"{""FileSize"": 1024, ""LogLevel"": ""Error""}")]
    [InlineData(@"{""LogDirectory"": ""Diagnostics"", ""LogLevel"": ""Error""}")]
    [InlineData(@"{""LogDirectory"": ""Diagnostics"", ""FileSize"": 1024}")]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters - yes, it does use.
    public void SelfDiagnosticsConfigParser_TryGetConfiguration_AnyFieldMissing_Fails(string configFileContent)
#pragma warning restore xUnit1026 // Theory methods should use all of their parameters
    {
        var sut = new Mock<SelfDiagnosticsConfigParser>();
        sut.Setup(c => c.TryReadConfigFile(It.IsAny<string>(), out configFileContent)).Returns(true);

        Assert.False(sut.Object.TryGetConfiguration(out _, out _, out _));
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryGetConfiguration_FileSizeLowerThanLimit_AdjustsToLimit()
    {
        var configFileContent = @"{""LogDirectory"": ""Diagnostics"", ""FileSize"": 1023, ""LogLevel"": ""Error""}";
        var sut = new Mock<SelfDiagnosticsConfigParser>();
        sut.Setup(c => c.TryReadConfigFile(It.IsAny<string>(), out configFileContent)).Returns(true);
        sut.Setup(c => c.SetFileSizeWithinLimit(It.IsAny<int>())).CallBase();

        var result = sut.Object.TryGetConfiguration(out _, out var fileSize, out _);

        Assert.True(result);
        Assert.Equal(SelfDiagnosticsConfigParser.FileSizeLowerLimit, fileSize);
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryGetConfiguration_FileSizeHigherThanLimit_AdjustsToLimit()
    {
        var configFileContent = @"{""LogDirectory"": ""Diagnostics"", ""FileSize"": 133000, ""LogLevel"": ""Error""}";
        var sut = new Mock<SelfDiagnosticsConfigParser>();
        sut.Setup(c => c.TryReadConfigFile(It.IsAny<string>(), out configFileContent)).Returns(true);
        sut.Setup(c => c.SetFileSizeWithinLimit(It.IsAny<int>())).CallBase();

        var result = sut.Object.TryGetConfiguration(out _, out var fileSize, out _);

        Assert.True(result);
        Assert.Equal(SelfDiagnosticsConfigParser.FileSizeUpperLimit, fileSize);
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryReadConfigFile_ExceptionThrown_ReturnsFalse()
    {
        CreateConfigFile(SelfDiagnosticsConfigParser.ConfigFileName);
        using var file = File.Open(SelfDiagnosticsConfigParser.ConfigFileName, FileMode.Open, FileAccess.Read,
            FileShare.None); // file is open, so opening it again in SelfDiagnosticsConfigParser will throw.

        var parser = new SelfDiagnosticsConfigParser();

        Assert.False(parser.TryReadConfigFile(SelfDiagnosticsConfigParser.ConfigFileName, out _));

        file.Close();
        CleanupConfigFile(SelfDiagnosticsConfigParser.ConfigFileName);
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryReadConfigFileFromCurrentFolder_SuccessfullyReadsFile()
    {
        const string ConfigFileName = "testConfig.json";
        CreateConfigFile(ConfigFileName);

        var parser = new SelfDiagnosticsConfigParser();

        Assert.True(parser.TryReadConfigFile(ConfigFileName, out _));

        CleanupConfigFile(ConfigFileName);
    }

    [Fact]
    public void SelfDiagnosticsConfigParser_TryReadConfigFileFromAppDirectory_SuccessfullyReadsFile()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var directoryInfo = Directory.CreateDirectory("test");
        var configFilePath = Path.Combine(AppContext.BaseDirectory, SelfDiagnosticsConfigParser.ConfigFileName);
        CreateConfigFile(configFilePath);

        Directory.SetCurrentDirectory(directoryInfo.FullName);
        var parser = new SelfDiagnosticsConfigParser();
        Assert.True(parser.TryReadConfigFile(SelfDiagnosticsConfigParser.ConfigFileName, out _));
        Directory.SetCurrentDirectory(currentDir);

        CleanupConfigFile(configFilePath);
        Directory.Delete(directoryInfo.FullName);
    }

    private static void CreateConfigFile(string configFilePath)
    {
        using FileStream file = File.Open(configFilePath, FileMode.Create, FileAccess.Write);
        file.Write(new byte[] { 0x5C, 0x75, 0x46, 0x46, 0x30, 0x30 }, 0, 6);
    }

    private static void CleanupConfigFile(string configFilePath)
    {
        if (File.Exists(configFilePath))
        {
            File.Delete(configFilePath);
        }
    }
}
