// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Microsoft.Shared.Data.Validation.Test;

public class LengthAttributeTests
{
    public class TestOptions
    {
        [Length(5)]
        public string? Name { get; set; }

        [Length(5, 7)]
        public string? Address { get; set; }
    }

    [Fact]
    public void Basic()
    {
        var options = new TestOptions();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Assertions on null values.
        options.Name = null;
        options.Address = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum only without upper bound.
        options.Name = "abcd";
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Name), results[0].MemberNames);
        Assert.Contains(nameof(options.Name), results[0].ErrorMessage);

        options.Name = "abcde";
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum and maximum.
        options.Name = null;
        options.Address = "abcd";
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Address), results[0].MemberNames);
        Assert.Contains(nameof(options.Address), results[0].ErrorMessage);

        options.Address = "abcdefghi";
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Address), results[0].MemberNames);
        Assert.Contains(nameof(options.Address), results[0].ErrorMessage);

        options.Address = "abcdefg";
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class TestOptionsExclusive
    {
        [Length(5, Exclusive = true)]
        public string? Name { get; set; }

        [Length(5, 7, Exclusive = true)]
        public string? Address { get; set; }
    }

    [Fact]
    public void BasicWithExclusive()
    {
        var options = new TestOptionsExclusive();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Assertions on null values.
        options.Name = null;
        options.Address = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum only without upper bound.
        options.Name = "abcde";
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Name), results[0].MemberNames);
        Assert.Contains(nameof(options.Name), results[0].ErrorMessage);

        options.Name = "abcdef";
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        // Assertions on values bounded by minimum and maximum.
        options.Name = null;
        options.Address = "abcdefg";
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Address), results[0].MemberNames);
        Assert.Contains(nameof(options.Address), results[0].ErrorMessage);

        options.Name = null;
        options.Address = "abcde";
        results.Clear();
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Address), results[0].MemberNames);
        Assert.Contains(nameof(options.Address), results[0].ErrorMessage);

        options.Address = "abcdef";
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class Enumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return i;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count { get; set; }
    }

    public class TestOptionsEnumerable
    {
        [Length(5)]
        public Enumerable? Names { get; set; }

        [Length(5, 7)]
        public Enumerable? Addresses { get; set; }
    }

    [Fact]
    public void BasicEnumerable()
    {
        var options = new TestOptionsEnumerable();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        options.Names = null;
        options.Addresses = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Names = new Enumerable
        {
            Count = 4,
        };
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Names), results[0].MemberNames);
        Assert.Contains(nameof(options.Names), results[0].ErrorMessage);

        options.Names = new Enumerable
        {
            Count = 5,
        };
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Names = null;
        options.Addresses = new Enumerable
        {
            Count = 8,
        };
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Addresses), results[0].MemberNames);
        Assert.Contains(nameof(options.Addresses), results[0].ErrorMessage);

        options.Addresses = new Enumerable
        {
            Count = 7,
        };
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class TestOptionsCollection
    {
        [Length(5)]
        public ICollection? Names { get; set; }

        [Length(5, 7)]
        public ICollection? Addresses { get; set; }
    }

    [Fact]
    public void BasicCollection()
    {
        var options = new TestOptionsCollection();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        options.Names = null;
        options.Addresses = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Names = new[] { "a", "b", "c", "d" };
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Names), results[0].MemberNames);
        Assert.Contains(nameof(options.Names), results[0].ErrorMessage);

        options.Names = new[] { "a", "b", "c", "d", "e" };
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Names = null;
        options.Addresses = new[] { "a", "b", "c", "d", "e", "f", "g", "h" };
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Addresses), results[0].MemberNames);
        Assert.Contains(nameof(options.Addresses), results[0].ErrorMessage);

        options.Addresses = new[] { "a", "b", "c", "d", "e", "f", "g" };
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class Countable
    {
        public int Count { get; set; }
    }

    public class TestOptionsCountable
    {
        [Length(5)]
        public Countable? Names { get; set; }

        [Length(5, 7)]
        public Countable? Addresses { get; set; }
    }

    [Fact]
    public void BasicCountable()
    {
        var options = new TestOptionsCountable();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        options.Names = null;
        options.Addresses = null;
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Names = new Countable
        {
            Count = 4,
        };
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Names), results[0].MemberNames);
        Assert.Contains(nameof(options.Names), results[0].ErrorMessage);

        options.Names = new Countable
        {
            Count = 5,
        };
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);

        options.Names = null;
        options.Addresses = new Countable
        {
            Count = 8,
        };
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.Single(results);
        Assert.Contains(nameof(options.Addresses), results[0].MemberNames);
        Assert.Contains(nameof(options.Addresses), results[0].ErrorMessage);

        options.Addresses = new Countable
        {
            Count = 7,
        };
        results.Clear();
        Assert.True(Validator.TryValidateObject(options, context, results, true));
        Assert.Empty(results);
    }

    public class BadOptions0
    {
        [Length(5)]
        public int Time { get; set; }
    }

    public class BadOptions1
    {
        [Length(-1)]
        public string? Time { get; set; }
    }

    public class BadOptions2
    {
        [Length(5, 10)]
        public int Time { get; set; }
    }

    public class BadOptions3
    {
        [Length(10, 5)]
        public int Time { get; set; }
    }

    public class BadOptions4
    {
        [Length(-5, -10)]
        public int Time { get; set; }
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

        var options2 = new BadOptions2();
        context = new ValidationContext(options2);
        results = new List<ValidationResult>();
        Assert.Throws<InvalidOperationException>(() => _ = Validator.TryValidateObject(options2, context, results, true));

        var options3 = new BadOptions3();
        context = new ValidationContext(options3);
        results = new List<ValidationResult>();
        Assert.Throws<InvalidOperationException>(() => _ = Validator.TryValidateObject(options3, context, results, true));

        var options4 = new BadOptions4();
        context = new ValidationContext(options4);
        results = new List<ValidationResult>();
        Assert.Throws<InvalidOperationException>(() => _ = Validator.TryValidateObject(options4, context, results, true));
    }

    [Fact]
    public void NakedContext()
    {
        var value = "abcd";
        var context = new ValidationContext(value);
        var attr = new LengthAttribute(5);

        var result = attr.GetValidationResult(value, context);

        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Empty(result!.MemberNames);
    }

    public class TestOptionsCustomMessage
    {
        [Length(5, ErrorMessage = "My custom message for '{0}'.")]
        public List<string> CustomMessage { get; set; } = new List<string>();
    }

    [Fact]
    public void CustomErrorMessage()
    {
        var options = new TestOptionsCustomMessage();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Assertions on null values.
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.NotEmpty(results);

        Assert.Equal("My custom message for 'CustomMessage'.", results.Single().ErrorMessage);
    }

    public class TestOptionsDefaultMessage
    {
        [Length(5)]
        public List<string> DefaultMessage { get; set; } = new List<string>();
    }

    [Fact]
    public void DefaultErrorMessage()
    {
        var options = new TestOptionsDefaultMessage();
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        // Assertions on null values.
        Assert.False(Validator.TryValidateObject(options, context, results, true));
        Assert.NotEmpty(results);

        Assert.Equal("The field DefaultMessage length must be greater or equal than 5.", results.Single().ErrorMessage);
    }
}
