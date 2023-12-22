// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.LocalAnalyzers.Resource.Test;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.Test;

public class InternalReferencedInPublicDocAnalyzerTests
{
    private static readonly string[] _members =
    {
        "void Method() {}", "int Property {get; set;}", "event System.EventHandler Event;", "int _field;", "string this[int i] { get => string.Empty; set {} }",
    };
    private static readonly string[] _membersReferenced = { "void Referenced() {}", "int Referenced {get; set;}", "event System.EventHandler Referenced;", "int Referenced;", };
    private static readonly string[] _typesReferenced = { "class Referenced {}", "struct Referenced {}", "interface Referenced {}", "delegate void Referenced(int value);", "enum Referenced {}", };
    private static readonly string[] _compositeTypeNames = { "class", "struct", "interface" };

    public static IEnumerable<Assembly> References => new Assembly[]
    {
        // add references here
    };

    public static IEnumerable<object[]> GetMemberAndTypePairs(string memberAccessModifier, string typeAccessModifier)
    {
        return MakePairs(memberAccessModifier, _members, typeAccessModifier, _typesReferenced);
    }

    public static IEnumerable<object[]> GetCompositeTypeAndMemberPairs(string typeAccessModifier, string memberAccessModifier)
    {
        return MakePairs(typeAccessModifier, _compositeTypeNames, memberAccessModifier, _membersReferenced);
    }

    private static IEnumerable<object[]> MakePairs(string firstPrefix, IReadOnlyList<string> firstList, string secondPrefix, IReadOnlyList<string> secondList)
    {
        var casesCnt = Math.Max(firstList.Count, secondList.Count);
        for (var i = 0; i < casesCnt; i++)
        {
            var first = $"{firstPrefix} {firstList[i % firstList.Count]}";
            var second = $"{secondPrefix} {secondList[i % secondList.Count]}";
            yield return new object[] { first, second };
        }
    }

    private static string MemberReferencesTopLevelTypeLine6(string classAccess, string member, string type)
    {
        return @"
            namespace Example;
        
            " + classAccess + @" class TestClass
            {
                /// <summary>
                /// Does something with <see cref=""Referenced""/>. This is line #6.
                /// </summary>
                " + member + @"
            }

            " + type + @"
            ";
    }

    private static string TopLevelTypeReferencesItsMemberLine4(string type, string member)
    {
        return @"
            namespace Example
            {
                /// <summary>
                /// Does something with <see cref=""Referenced""/>. This is line #4.
                /// </summary>
                " + type + @" Test
                {
                    " + member + @"
                }
            }
            ";
    }

    [Fact]
    public void CheckExceptionIsThrownWhenNullIsPassedToInitializeCall()
    {
        var a = new InternalReferencedInPublicDocAnalyzer();
        Assert.Throws<ArgumentNullException>(() => a.Initialize(null!));
    }

    [Theory]
    [MemberData(nameof(GetMemberAndTypePairs), "public", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "public", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected internal", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected internal", "")]
    public async Task ShouldIndicateWhenExternallyVisibleMemberReferencesTopLevelInternalType(string member, string type)
    {
        var source = MemberReferencesTopLevelTypeLine6("public", member, type);

        var result = await Analyze(source);

        AssertDetected(result, source, 6, "Referenced");
    }

    [Theory]
    [MemberData(nameof(GetMemberAndTypePairs), "public", "public")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected", "public")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected internal", "public")]
    [MemberData(nameof(GetMemberAndTypePairs), "private", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "private", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "private protected", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "private protected", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "internal", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "internal", "")]
    public async Task ShouldNotIndicateWhenMemberReferencesTopLevelType(string member, string type)
    {
        var source = MemberReferencesTopLevelTypeLine6("public", member, type);

        var result = await Analyze(source);

        AssertNotDetected(result);
    }

    [Theory]
    [MemberData(nameof(GetMemberAndTypePairs), "public", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "public", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "public", "public")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected", "public")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected internal", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected internal", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "protected internal", "public")]
    [MemberData(nameof(GetMemberAndTypePairs), "private", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "private", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "private protected", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "private protected", "")]
    [MemberData(nameof(GetMemberAndTypePairs), "internal", "internal")]
    [MemberData(nameof(GetMemberAndTypePairs), "internal", "")]
    public async Task ShouldNotIndicateWhenInternalClassMemberReferencesTopLevelType(string member, string type)
    {
        var source = MemberReferencesTopLevelTypeLine6("internal", member, type);

        var result = await Analyze(source);

        AssertNotDetected(result);
    }

    [Theory]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "public", "internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "public", "private")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "public", "private protected")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected", "internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected", "private")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected", "private protected")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected internal", "internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected internal", "private")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected internal", "private protected")]
    public async Task ShouldIndicateWhenExternallyVisibleTopLevelTypeReferencesItsInvisibleMember(string type, string member)
    {
        var source = TopLevelTypeReferencesItsMemberLine4(type, member);

        var result = await Analyze(source);

        AssertDetected(result, source, 4, "Referenced");
    }

    [Theory]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "public", "public")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "public", "protected")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "public", "protected internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected", "public")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected", "protected")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected", "protected internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected internal", "public")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected internal", "protected")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "protected internal", "protected internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "internal", "public")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "internal", "internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "internal", "protected")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "internal", "protected internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "internal", "private")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "internal", "private protected")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "", "public")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "", "internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "", "protected")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "", "protected internal")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "", "private")]
    [MemberData(nameof(GetCompositeTypeAndMemberPairs), "", "private protected")]
    public async Task ShouldNotIndicateWhenTopLevelTypeReferencesItsMember(string type, string member)
    {
        var source = TopLevelTypeReferencesItsMemberLine4(type, member);

        var result = await Analyze(source);

        AssertNotDetected(result);
    }

    [Theory]
    [InlineData("public", false)]
    [InlineData("internal", true)]
    [InlineData("", true)]
    public async Task ShouldSupportReferencesToEnumMembers(string enumAccess, bool shouldIndicate)
    {
        var source = @"
            namespace Example
            {
                /// <summary>
                /// Use <see cref=""MyEnum.Member1""/>. This is line #4.
                /// </summary>
                public class TestClass
                {
                    public void Method X() {}
                }

                " + enumAccess + @" enum MyEnum
                {
                    Member1,
                    Member2,
                }
            }
            ";

        var result = await Analyze(source);

        if (shouldIndicate)
        {
            AssertDetected(result, source, 4, "MyEnum.Member1");
        }
        else
        {
            AssertNotDetected(result);
        }
    }

    [Theory]
    [InlineData("public", "public", false)]
    [InlineData("public", "internal", true)]
    [InlineData("internal", "public", false)]
    [InlineData("", "internal", false)]
    public async Task ShouldSupportCommentsOnEnumMembers(string enumAccess, string typeAccess, bool shouldIndicate)
    {
        var source = @"
            namespace Example
            {
                " + typeAccess + @" class Referenced
                {
                    public void Method X() {}
                }

                " + enumAccess + @" enum MyEnum
                {
                    Member1,

                    /// <summary>
                    /// Uses <see cref=""Referenced""/>. This is line #13.
                    /// </summary>
                    Member2,
                }
            }
            ";

        var result = await Analyze(source);

        if (shouldIndicate)
        {
            AssertDetected(result, source, 13, "Referenced");
        }
        else
        {
            AssertNotDetected(result);
        }
    }

    [Theory]
    [InlineData("public", "public", false)]
    [InlineData("public", "internal", true)]
    [InlineData("public", "private", true)]
    [InlineData("public", "protected", false)]
    [InlineData("public", "protected internal", false)]
    [InlineData("public", "private protected", true)]
    [InlineData("internal", "public", true)]
    [InlineData("internal", "internal", true)]
    [InlineData("internal", "private", true)]
    [InlineData("internal", "protected", true)]
    [InlineData("internal", "protected internal", true)]
    [InlineData("internal", "private protected", true)]
    [InlineData("", "public", true)]
    [InlineData("", "internal", true)]
    [InlineData("", "private", true)]
    [InlineData("", "protected", true)]
    [InlineData("", "protected internal", true)]
    [InlineData("", "private protected", true)]
    public async Task ShouldSupportCrefPointingToNestedType(string enclosingTypeAccess, string nestedTypeAccess, bool shouldIndicate)
    {
        var source = @"
            namespace Example
            {
                " + enclosingTypeAccess + @" class Enclosing
                {
                    " + nestedTypeAccess + @" class Referenced
                    {
                    }
                }

                /// <summary>
                /// Uses <see cref=""Enclosing.Referenced""/>. This is line #11.
                /// </summary>
                public interface ITest
                {
                }
            }
            ";

        var result = await Analyze(source);

        if (shouldIndicate)
        {
            AssertDetected(result, source, 11, "Enclosing.Referenced");
        }
        else
        {
            AssertNotDetected(result);
        }
    }

    [Theory]
    [InlineData("public", "public", true)]
    [InlineData("public", "internal", false)]
    [InlineData("public", "private", false)]
    [InlineData("public", "protected", true)]
    [InlineData("public", "protected internal", true)]
    [InlineData("public", "private protected", false)]
    [InlineData("internal", "public", false)]
    [InlineData("internal", "internal", false)]
    [InlineData("internal", "private", false)]
    [InlineData("internal", "protected", false)]
    [InlineData("internal", "protected internal", false)]
    [InlineData("internal", "private protected", false)]
    [InlineData("", "public", false)]
    [InlineData("", "internal", false)]
    [InlineData("", "private", false)]
    [InlineData("", "protected", false)]
    [InlineData("", "protected internal", false)]
    [InlineData("", "private protected", false)]
    public async Task ShouldSupportCrefOnNestedType(string enclosingTypeAccess, string nestedTypeAccess, bool shouldIndicate)
    {
        var source = @"
            namespace Example
            {
                " + enclosingTypeAccess + @" class Enclosing
                {
                    /// <summary>
                    /// Uses <see cref=""Referenced""/>. This is line #6.
                    /// </summary>
                    " + nestedTypeAccess + @" class Test
                    {
                    }
                }

                internal interface Referenced
                {
                }
            }
            ";

        var result = await Analyze(source);

        if (shouldIndicate)
        {
            AssertDetected(result, source, 6, "Referenced");
        }
        else
        {
            AssertNotDetected(result);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("NotExists")]
    [InlineData("this is not a valid reference")]
    [InlineData("$^&#@")]
    public async Task ShouldNotIndicateWhenCrefIsInvalid(string cref)
    {
        var source = @"
            namespace Example
            {
                public class TestClass
                {
                    /// <summary>
                    /// Use <see cref=""" + cref + @"""/>.
                    /// </summary>
                    public void Method X() {}
                }
            }
            ";

        var result = await Analyze(source);

        AssertNotDetected(result);
    }

    [Fact]
    public async Task ShouldNotIndicateWhenCommentIsOrphan()
    {
        var source = @"
            namespace Example
            {
                public class TestClass
                {
                    /// <summary>
                    /// Use <see cref=""Referenced""/>.
                    /// </summary>                    
                }

                internal class Referenced {}
            }
            ";

        var result = await Analyze(source);

        AssertNotDetected(result);
    }

    [Fact]
    public async Task ShouldNotIndicateWhenCRefDoesNotBelongToXmlDocumentation()
    {
        var source = @"
            namespace Example
            {
                /*
                Type <see cref=""Referenced""/> is referenced
                */
                public class TestClass
                {
                    // Use <see cref=""Referenced""/>
                    public void Method() {};
                }

                internal class Referenced {}
            }
            ";

        var result = await Analyze(source);

        AssertNotDetected(result);
    }

    private static void AssertDetected(IReadOnlyList<Diagnostic> result, string source, int lineNumber, string detectedText) =>
        AssertDetected(result, source, new[] { lineNumber }, new[] { detectedText });

    private static void AssertDetected(IReadOnlyList<Diagnostic> result, string source, int[] lineNumbers, string[] detectedTexts)
    {
        Debug.Assert(lineNumbers.Length == detectedTexts.Length, "Line numbers and texts should be the same length");

        var detected = result.Where(IsInternalReferencedInPublicDocDiagnostic).ToList();

        var expectedNumberOfWarnings = lineNumbers.Length;
        Assert.Equal(expectedNumberOfWarnings, detected.Count);

        for (int i = 0; i < detected.Count; i++)
        {
            var location = detected[i].Location;
            Assert.Equal(lineNumbers[i], location.GetLineSpan().StartLinePosition.Line);

            var text = source.Substring(location.SourceSpan.Start, location.SourceSpan.Length);
            Assert.Equal(detectedTexts[i], text, StringComparer.Ordinal);
        }
    }

    private static bool IsInternalReferencedInPublicDocDiagnostic(Diagnostic d) => ReferenceEquals(d.Descriptor, DiagDescriptors.InternalReferencedInPublicDoc);

    private static void AssertNotDetected(IReadOnlyList<Diagnostic> result)
    {
        var detected = result.Where(IsInternalReferencedInPublicDocDiagnostic);
        Assert.Empty(detected);
    }

    private static async Task<IReadOnlyList<Diagnostic>> Analyze(string source)
    {
        return await RoslynTestUtils.RunAnalyzer(
            new InternalReferencedInPublicDocAnalyzer(),
            References,
            new[] { source }).ConfigureAwait(false);
    }
}
