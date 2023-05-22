// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.EnumStrings;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.EnumStrings.Test;

public static class ParserTests
{
    [Fact]
    public static async Task InvalidTypeLevelUses()
    {
        var args = new[]
        {
            ( "EnumStrings(typeof(string))", DiagDescriptors.IncorrectOverload ),
            ( "EnumStrings(ExtensionClassName = \"A.B\")", DiagDescriptors.InvalidExtensionClassName ),
            ( "EnumStrings(ExtensionClassName = \"123\")", DiagDescriptors.InvalidExtensionClassName ),
            ( "EnumStrings(ExtensionMethodName = \"A.B\")", DiagDescriptors.InvalidExtensionMethodName ),
            ( "EnumStrings(ExtensionMethodName = \"123\")", DiagDescriptors.InvalidExtensionMethodName ),
            ( "EnumStrings(ExtensionNamespace = \"A.123\")", DiagDescriptors.InvalidExtensionNamespace ),
            ( "EnumStrings(ExtensionNamespace = \"123\")", DiagDescriptors.InvalidExtensionNamespace ),
        };

        foreach (var (attrArg, diag) in args)
        {
            var source = @$"
                using Microsoft.Extensions.EnumStrings;

                namespace Test
                {{
                    [/*0+*/{attrArg}/*-0*/]
                    public enum Color
                    {{
                        Red,
                        Green,
                        Blue,
                    }}
                }}
            ";

            var (d, _) = await RoslynTestUtils.RunGenerator(
                new Generator(),
                new[] { Assembly.GetAssembly(typeof(EnumStringsAttribute))! },
                new[] { source }).ConfigureAwait(false);

            Assert.Equal(1, d.Count);
            source.AssertDiagnostic(0, diag, d[0]);

            (d, _) = await RoslynTestUtils.RunGenerator(
                new Generator(),
                new[]
                {
                    Assembly.GetAssembly(typeof(EnumStringsAttribute))!,
                    Assembly.GetAssembly(typeof(System.Collections.Frozen.FrozenDictionary))!,
                },
                new[] { source }).ConfigureAwait(false);

            Assert.Equal(1, d.Count);
            source.AssertDiagnostic(0, diag, d[0]);
        }
    }

    [Fact]
    public static async Task InvalidAssemblyLevelUses()
    {
        var args = new[]
        {
            ( "EnumStrings", DiagDescriptors.IncorrectOverload ),
            ( "EnumStrings(typeof(MyClass))", DiagDescriptors.InvalidEnumType ),
            ( "EnumStrings(typeof(MyJunk))", DiagDescriptors.InvalidEnumType ),
            ( "EnumStrings(typeof(Color), ExtensionClassName = \"A.B\")", DiagDescriptors.InvalidExtensionClassName ),
            ( "EnumStrings(typeof(Color), ExtensionClassName = \"123\")", DiagDescriptors.InvalidExtensionClassName ),
            ( "EnumStrings(typeof(Color), ExtensionMethodName = \"A.B\")", DiagDescriptors.InvalidExtensionMethodName ),
            ( "EnumStrings(typeof(Color), ExtensionMethodName = \"123\")", DiagDescriptors.InvalidExtensionMethodName ),
            ( "EnumStrings(typeof(Color), ExtensionNamespace = \"A.123\")", DiagDescriptors.InvalidExtensionNamespace ),
            ( "EnumStrings(typeof(Color), ExtensionNamespace = \"123\")", DiagDescriptors.InvalidExtensionNamespace ),
        };

        foreach (var (attrArg, diag) in args)
        {
            var source = @$"
                using Microsoft.Extensions.EnumStrings;

                [assembly: /*0+*/{attrArg}/*-0*/]

                public class MyClass
                {{
                }}

                public enum Color
                {{
                    Red,
                    Green,
                    Blue,
                }}
            ";

            var (d, _) = await RoslynTestUtils.RunGenerator(
                new Generator(),
                new[] { Assembly.GetAssembly(typeof(EnumStringsAttribute))! },
                new[] { source }).ConfigureAwait(false);

            Assert.Equal(1, d.Count);
            source.AssertDiagnostic(0, diag, d[0]);
        }
    }
}
