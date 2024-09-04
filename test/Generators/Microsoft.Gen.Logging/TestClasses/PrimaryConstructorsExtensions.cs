// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

#pragma warning disable SA1402

namespace TestClasses
{
    public partial class ClassWithPrimaryConstructor(ILogger logger)
    {
        [LoggerMessage(0, LogLevel.Debug, "Test.")]
        public partial void Test();
    }

    public partial class ClassWithPrimaryConstructorInDifferentPartialDeclaration(ILogger logger);

    public partial class ClassWithPrimaryConstructorInDifferentPartialDeclaration
    {
        [LoggerMessage(0, LogLevel.Debug, "Test.")]
        public partial void Test();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Used in generated code")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S3604:Member initializer values should not be redundant", Justification = "Used for testing")]
    public partial class ClassWithPrimaryConstructorAndField(ILogger logger)
    {
        private readonly ILogger _logger = logger;

        [LoggerMessage(0, LogLevel.Debug, "Test.")]
        public partial void Test();
    }

    public partial record RecordWithPrimaryConstructor(ILogger Logger)
    {
        [LoggerMessage(0, LogLevel.Debug, "Test.")]
        public partial void Test();
    }
}
