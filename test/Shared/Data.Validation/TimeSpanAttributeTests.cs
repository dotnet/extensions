// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.Shared.Data.Validation.Test;

public class TimeSpanAttributeTests
{
    public class TestOptions
    {
        [TimeSpan(0, 10)]
        public TimeSpan? Time { get; set; }

        [TimeSpan(0)]
        public TimeSpan? Time2 { get; set; }
    }

    [Fact]
    public void Basic()
    {
        var options = new TestOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Assertions on null values.
        options.Time = null;
        options.Time2 = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum and maximum.
        options.Time = TimeSpan.FromTicks(-1);
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time), results[0].MemberNames);
        Assert.Contains(nameof(options.Time), results[0].ErrorMessage);

        options.Time = TimeSpan.FromTicks(TimeSpan.FromMilliseconds(10).Ticks + 1);
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time), results[0].MemberNames);
        Assert.Contains(nameof(options.Time), results[0].ErrorMessage);

        options.Time = TimeSpan.FromTicks(0);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Time = TimeSpan.FromMilliseconds(10);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum only without upper bound.
        options.Time = null;
        options.Time2 = TimeSpan.FromTicks(-1);
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time2), results[0].MemberNames);
        Assert.Contains(nameof(options.Time2), results[0].ErrorMessage);

        options.Time2 = TimeSpan.FromTicks(0);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Time2 = TimeSpan.FromMilliseconds(int.MaxValue);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class TestOptionsExclusive
    {
        [TimeSpan(0, 10, Exclusive = true)]
        public TimeSpan? Time { get; set; }

        [TimeSpan(0, Exclusive = true)]
        public TimeSpan? Time2 { get; set; }
    }

    [Fact]
    public void BasicWithExclusive()
    {
        var options = new TestOptionsExclusive();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Assertions on null values.
        options.Time = null;
        options.Time2 = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum and maximum.
        options.Time = TimeSpan.FromTicks(0);
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time), results[0].MemberNames);
        Assert.Contains(nameof(options.Time), results[0].ErrorMessage);

        options.Time = TimeSpan.FromMilliseconds(10);
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time), results[0].MemberNames);
        Assert.Contains(nameof(options.Time), results[0].ErrorMessage);

        options.Time = TimeSpan.FromMilliseconds(5);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum only without upper bound.
        options.Time = null;
        options.Time2 = TimeSpan.FromTicks(-1);
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time2), results[0].MemberNames);
        Assert.Contains(nameof(options.Time2), results[0].ErrorMessage);

        options.Time2 = TimeSpan.FromTicks(0);
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time2), results[0].MemberNames);
        Assert.Contains(nameof(options.Time2), results[0].ErrorMessage);

        options.Time2 = TimeSpan.FromMilliseconds(int.MaxValue);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class TestOptionsString
    {
        [TimeSpan("00:00:00", "00:00:00.01")]
        public TimeSpan? Time { get; set; }

        [TimeSpan("00:00:00")]
        public TimeSpan? Time2 { get; set; }
    }

    [Fact]
    public void BasicString()
    {
        var options = new TestOptionsString();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Assertions on null values.
        options.Time = null;
        options.Time2 = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum and maximum.
        options.Time = TimeSpan.FromTicks(-1);
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time), results[0].MemberNames);
        Assert.Contains(nameof(options.Time), results[0].ErrorMessage);

        options.Time = TimeSpan.FromTicks(TimeSpan.FromMilliseconds(10).Ticks + 1);
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time), results[0].MemberNames);
        Assert.Contains(nameof(options.Time), results[0].ErrorMessage);

        options.Time = TimeSpan.FromTicks(0);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Time = TimeSpan.FromMilliseconds(10);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum only without upper bound.
        options.Time = null;
        options.Time2 = TimeSpan.FromTicks(-1);
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Time2), results[0].MemberNames);
        Assert.Contains(nameof(options.Time2), results[0].ErrorMessage);

        options.Time2 = TimeSpan.FromTicks(0);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Time2 = TimeSpan.FromMilliseconds(int.MaxValue);
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class BadOptions0
    {
        [TimeSpan(0, 10)]
        public int Time { get; set; }
    }

    public class BadOptions1
    {
        [TimeSpan(10, 9)]
        public TimeSpan Time { get; set; }
    }

    public class BadOptions3
    {
        [TimeSpan(10, 10)]
        public TimeSpan Time { get; set; }
    }

    [Fact]
    public void BadAttributeUse()
    {
        var options0 = new BadOptions0();
        var context = new ValidationContext(options0);
        var results = new List<ValidationResult>();
        Assert.Throws<InvalidOperationException>(() => _ = Validator.TryValidateObject(options0, context, results, true));

        var options1 = new BadOptions1();
        context = new ValidationContext(options1);
        results = new List<ValidationResult>();
        Assert.Throws<InvalidOperationException>(() => _ = Validator.TryValidateObject(options1, context, results, true));

        var options3 = new BadOptions3();
        context = new ValidationContext(options3);
        results = new List<ValidationResult>();
        Assert.Throws<InvalidOperationException>(() => _ = Validator.TryValidateObject(options3, context, results, true));
    }

    [Fact]
    public void NakedContext()
    {
        var value = TimeSpan.FromTicks(-1);
        var context = new ValidationContext(value);
        var attr = new TimeSpanAttribute(0, 10);

        var result = attr.GetValidationResult(value, context);

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Empty(result!.MemberNames);
    }
}
