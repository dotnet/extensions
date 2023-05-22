// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Microsoft.Shared.Data.Validation.Test;

public class ExclusiveRangeAttributeTests
{
    public class TestOptions0
    {
        [ExclusiveRange(0, 10)]
        public int? Number { get; set; }
    }

    [Fact]
    public void Basic()
    {
        var options = new TestOptions0();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        options.Number = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Number = 0;
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Number), results[0].MemberNames);
        Assert.Contains(nameof(options.Number), results[0].ErrorMessage);

        options.Number = 10;
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Number), results[0].MemberNames);
        Assert.Contains(nameof(options.Number), results[0].ErrorMessage);

        options.Number = 1;
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Number = 9;
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class TestOptionsDouble
    {
        [ExclusiveRange(0.0, 10.0)]
        public double? Number { get; set; }
    }

    [Fact]
    public void BasicDouble()
    {
        var options = new TestOptionsDouble();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        options.Number = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Number = 0;
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Number), results[0].MemberNames);
        Assert.Contains(nameof(options.Number), results[0].ErrorMessage);

        options.Number = 10;
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Number), results[0].MemberNames);
        Assert.Contains(nameof(options.Number), results[0].ErrorMessage);

        options.Number = 0.00001;
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Number = 10.0 - 0.00001;
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class BadOptions1
    {
        [ExclusiveRange(10.0, 9)]
        public int? Number { get; set; }
    }

    [Fact]
    public void BadAttributeUse()
    {
        var options1 = new BadOptions1
        {
            Number = 4,
        };
        var context = new ValidationContext(options1);
        var results = new List<ValidationResult>();
        Assert.Throws<InvalidOperationException>(() => _ = Validator.TryValidateObject(options1, context, results, true));
    }

    public class BadOptions2
    {
        [ExclusiveRange(10, 10)]
        public int? Number { get; set; }
    }

    [Fact]
    public void BadAttributeUse_MinEqualsMax()
    {
        var options1 = new BadOptions2
        {
            Number = 10,
        };
        var context = new ValidationContext(options1);
        var results = new List<ValidationResult>();
        Assert.Throws<InvalidOperationException>(() => _ = Validator.TryValidateObject(options1, context, results, true));
    }

    [Fact]
    public void NakedContext()
    {
        var value = 0.0;
        var context = new ValidationContext(value);
        var attr = new ExclusiveRangeAttribute(0.0, 10.0);

        var result = attr.GetValidationResult(value, context);

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Empty(result!.MemberNames);
    }
}
