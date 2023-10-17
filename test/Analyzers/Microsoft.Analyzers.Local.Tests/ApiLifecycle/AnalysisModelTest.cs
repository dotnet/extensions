// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Model;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Test;

public class AnalysisModelTest
{
    [Fact]
    public void Field_Fallbacks_To_NotNull_Defaults_When_Value_Not_Found_In_Json()
    {
        var field = new Field([]);

        Assert.Equal(string.Empty, field.Member);
        Assert.Equal(Stage.Experimental, field.Stage);
    }

    [Fact]
    public void PublicMember_Fallbacks_To_NotNull_Defaults_When_Value_Not_Found_In_Json()
    {
        var member = new TypeDef([]);

        Assert.Equal(string.Empty, member.ModifiersAndName);
        Assert.Equal(Stage.Experimental, member.Stage);
        Assert.Equal(Array.Empty<Field>(), member.Fields);
        Assert.Equal(Array.Empty<string>(), member.BaseTypes);
        Assert.Equal(Array.Empty<string>(), member.Constraints);
        Assert.Equal(Array.Empty<Method>(), member.Methods);
        Assert.Equal(Array.Empty<Prop>(), member.Properties);
    }

    [Fact]
    public void Prop_Fallbacks_To_NotNull_Defaults_When_Value_Not_Found_In_Json()
    {
        var prop = new Prop([]);

        Assert.Equal(string.Empty, prop.Member);
        Assert.Equal(Stage.Experimental, prop.Stage);
    }

    [Fact]
    public void PackageAnalysis_Fallbacks_To_NotNull_Defaults_When_Value_Not_Found_In_Json()
    {
        var analysis = new AssemblyAnalysis(Assembly.Empty);

        Assert.Equal(Assembly.Empty, analysis.Assembly);
        Assert.Empty(analysis.MissingProperties);
        Assert.Empty(analysis.MissingBaseTypes);
        Assert.Empty(analysis.FoundInBaseline);
        Assert.Empty(analysis.NotFoundInBaseline);
        Assert.Empty(analysis.MissingTypes);
        Assert.Empty(analysis.MissingConstraints);
        Assert.Empty(analysis.MissingFields);
        Assert.Empty(analysis.MissingMethods);
        Assert.Empty(analysis.MissingProperties);
    }

    [Fact]
    public void Package_Fallbacks_To_NotNull_Defaults_When_Value_Not_Found_In_Json()
    {
        var package = new Assembly([]);

        Assert.Equal(Array.Empty<TypeDef>(), package.Types);
        Assert.Equal(string.Empty, package.Name);
    }

    [Fact]
    public void Method_FallbacksTo_NotNull_Defaults_When_Value_Not_Found_In_Json()
    {
        var method = new Method([]);

        Assert.Equal(string.Empty, method.Member);
        Assert.Equal(Stage.Experimental, method.Stage);
    }
}
