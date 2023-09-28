// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization.Test;

public class ExceptionSummaryTests
{
    [Fact]
    public void Equals_WhenComparingTwoIdentical_ReturnsTrue()
    {
        var exceptionSummary1 = new ExceptionSummary("ExceptionType1", "Exception Description 1", "Exception Additional Details 1");
        var exceptionSummary2 = new ExceptionSummary("ExceptionType1", "Exception Description 1", "Exception Additional Details 1");

        Assert.True(exceptionSummary1.Equals(exceptionSummary2));
        Assert.True(exceptionSummary2.Equals(exceptionSummary1));
        Assert.Equal(exceptionSummary1, exceptionSummary2);
        Assert.Equal(exceptionSummary1.GetHashCode(), exceptionSummary2.GetHashCode());

        Assert.True(exceptionSummary1 == exceptionSummary2);
        Assert.True(exceptionSummary1.Equals((object)exceptionSummary2));
        Assert.False(exceptionSummary1 != exceptionSummary2);
    }

    [Fact]
    public void Equals_WhenComparingTwoDifferent_ReturnsTrue()
    {
        var exceptionSummary1 = new ExceptionSummary("ExceptionType1", "Exception Description 1", "Exception Additional Details 1");
        var exceptionSummary2 = new ExceptionSummary("ExceptionType2", "Exception Description 2", "Exception Additional Details 2");

        Assert.False(exceptionSummary1.Equals(exceptionSummary2));
        Assert.False(exceptionSummary2.Equals(exceptionSummary1));
        Assert.NotEqual(exceptionSummary1, exceptionSummary2);
        Assert.NotEqual(exceptionSummary1.GetHashCode(), exceptionSummary2.GetHashCode());

        Assert.False(exceptionSummary1 == exceptionSummary2);
        Assert.False(exceptionSummary1.Equals((object)exceptionSummary2));
        Assert.False(exceptionSummary2.Equals((object)exceptionSummary1));
        Assert.True(exceptionSummary1 != exceptionSummary2);
    }

    [Fact]
    public void Equals_WhenComparingTwoDifferentObject_ReturnsTrue()
    {
        var exceptionSummary = new ExceptionSummary("ExceptionType1", "Exception Description 1", "Exception Additional Details 1");
        var exceptionTest = new ExceptionTest();

        Assert.False(exceptionSummary.Equals(exceptionTest));
        Assert.False(exceptionTest.Equals(exceptionSummary));
        Assert.NotEqual(exceptionSummary.GetHashCode(), exceptionTest.GetHashCode());
    }

    [Fact]
    public void Equals_Misc()
    {
        var s1 = new ExceptionSummary("One", "Two", "Three");
        var s2 = new ExceptionSummary("One", "Two", "Three");
        Assert.True(s1.Equals(s2));

        var s3 = new ExceptionSummary("One", "Two", "Three");
        var s4 = new ExceptionSummary("One", "Four", "Three");
        Assert.False(s3.Equals(s4));

        var s5 = new ExceptionSummary("One", "Four", "Three");
        var s6 = new ExceptionSummary("One", "Two", "Three");
        Assert.False(s5.Equals(s6));

        var s7 = new ExceptionSummary("One", "Two", "Three");
        var s8 = new ExceptionSummary("Four", "Two", "Three");
        Assert.False(s7.Equals(s8));

        var s9 = new ExceptionSummary("Four", "Two", "Three");
        var s10 = new ExceptionSummary("One", "Two", "Three");
        Assert.False(s9.Equals(s10));

        var s12 = new ExceptionSummary("One", "Two", "Three");
        var s13 = new ExceptionSummary("Four", "Five", "Six");
        Assert.False(s12.Equals(s13));
    }

    [Fact]
    public void ToStringTest()
    {
        var exceptionSummary1 = new ExceptionSummary("ExceptionType", "Exception Description", "Exception Long Description");
        Assert.Equal("ExceptionType:Exception Description:Exception Long Description", exceptionSummary1.ToString());

        Assert.Throws<ArgumentException>(() => new ExceptionSummary("", "Exception Description", "Exception Long Description"));
        Assert.Throws<ArgumentException>(() => new ExceptionSummary(" ", "Exception Description", "Exception Long Description"));

        Assert.Throws<ArgumentException>(() => new ExceptionSummary("ExceptionType", "", "Exception Long Description"));
        Assert.Throws<ArgumentException>(() => new ExceptionSummary("ExceptionType", " ", "Exception Long Description"));

        Assert.Throws<ArgumentNullException>(() => new ExceptionSummary("ExceptionType", "Exception Description", null!));
    }

    private class ExceptionTest
    {
    }
}
