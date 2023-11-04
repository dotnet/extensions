// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;

namespace TestClasses
{
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test code")]
    [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test code")]
    [SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Test code")]
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Test code")]
    [SuppressMessage("Major Code Smell", "S2376:Write-only properties should not be used", Justification = "Test code")]
    internal static partial class SensitiveRecordExtensions
    {
        internal const string Sensitive = "SENSITIVE";

        public record class BaseRecord
        {
            [PrivateData] public const string ConstFieldBase = Sensitive;
            [PrivateData] public static string StaticFieldBase = Sensitive;
            [PrivateData] public static string StaticPropBase => Sensitive;

#pragma warning disable CS0414 // The field is assigned but its value is never used
            [PrivateData] private readonly string _privateFieldBase = Sensitive;
            [PrivateData] private string _privateReadonlyFieldBase = Sensitive;
            [PrivateData] internal string InternalFieldBase = Sensitive;
#pragma warning restore CS0414 // The field is assigned but its value is never used
        }

        public record class InterimRecord : BaseRecord
        {
            [PrivateData] public virtual string PropVirtual => Sensitive;
        }

        // Even though this record and its base classes contain sensitive members,
        // they won't be logged because the default record's ToString() implementation doesn't emit non-public and non-instance members.
        internal record class MyRecord : InterimRecord
        {
            [PrivateData] public const string ConstField = Sensitive;
            [PrivateData] public static string StaticField = Sensitive;
            [PrivateData] public static string StaticProp => Sensitive;

#pragma warning disable CS0414 // The field is assigned but its value is never used
            [PrivateData] private string PrivateField = Sensitive;
            [PrivateData] internal string InternalField = Sensitive;
#pragma warning restore CS0414 // The field is assigned but its value is never used

            [PrivateData] private string PrivatePropGetSet { get; set; } = Sensitive;
            [PrivateData] internal string InternalPropGetSet { get; set; } = Sensitive;
            [PrivateData] protected string ProtectedPropGetSet { get; set; } = Sensitive;
            [PrivateData] private string PrivatePropGet => Sensitive;
            [PrivateData] internal string InternalPropGet => Sensitive;
            [PrivateData] protected string ProtectedPropGet => Sensitive;

            // This one overrides 'virtual' property declared in 'InterimRecord':
            public override string PropVirtual => "Regular";
        }

        internal record RecordWithSensitiveMembers(
            [PrivateData] string AnnotatedArgFromPrimaryCtor,
            [property: PrivateData] string AnnotatedPropFromPrimaryCtor)
        {
            [PrivateData]
            public string? PropGetSet { get; set; }
        }

        [LoggerMessage(LogLevel.Debug, "Param is {p0}")]
        public static partial void LogInTemplate(ILogger logger, MyRecord p0);

        [LoggerMessage(LogLevel.Debug)]
        public static partial void LogFullyStructured(ILogger logger, MyRecord p0);

        [LoggerMessage(LogLevel.Information, "Data was obtained")]
        public static partial void LogPropertiesWithTemplate(
            ILogger logger,
            [LogProperties] RecordWithSensitiveMembers data);

        [LoggerMessage(LogLevel.Information)]
        public static partial void LogPropertiesFullyStructured(
            ILogger logger,
            [LogProperties] RecordWithSensitiveMembers data);

        [LoggerMessage(LogLevel.Information, "Data is {data}")]
        public static partial void LogInTemplateWithAnnotation(
            ILogger logger,
            [PrivateData] RecordWithSensitiveMembers data);
    }
}
