// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

using System;
using System.Globalization;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.Json.Test;

public class JsonValueTest
{
    [Fact]
    public void TestJsonValueConstructors()
    {
        // JsonValue(bool?)
        Assert.True(new JsonValue(true).IsBoolean);
        Assert.True(new JsonValue(true).AsBoolean);
        Assert.True(new JsonValue(false).IsBoolean);
        Assert.False(new JsonValue(false).AsBoolean);
        Assert.True(new JsonValue(default(bool?)).IsNull);
        Assert.False(new JsonValue(default(bool?)).IsBoolean);
        Assert.False(new JsonValue(default(bool?)).AsBoolean);

        // JsonValue(double?)
        Assert.True(new JsonValue(1.0).IsNumber);
        Assert.Equal(1.0, new JsonValue(1.0).AsNumber);
        Assert.True(new JsonValue(default(double?)).IsNull);
        Assert.False(new JsonValue(default(double?)).IsNumber);
        Assert.Equal(0.0, new JsonValue(default(double?)).AsNumber);

        // JsonValue(string)
        Assert.True(new JsonValue(string.Empty).IsString);
        Assert.True(new JsonValue("text").IsString);
        Assert.Equal("text", new JsonValue("text").AsString);
        Assert.True(new JsonValue(default(string)).IsNull);
        Assert.False(new JsonValue(default(string)).IsString);
        Assert.Null(new JsonValue(default(string)).AsString);

        // JsonValue(JsonObject)
        Assert.True(new JsonValue(new JsonObject()).IsJsonObject);
        Assert.IsType<JsonObject>(new JsonValue(new JsonObject()).AsJsonObject);
        Assert.True(new JsonValue(default(JsonObject)).IsNull);
        Assert.False(new JsonValue(default(JsonObject)).IsJsonObject);
        Assert.Null(new JsonValue(default(JsonObject)).AsJsonObject);

        // JsonValue(JsonArray)
        Assert.True(new JsonValue(new JsonArray()).IsJsonArray);
        Assert.IsType<JsonArray>(new JsonValue(new JsonArray()).AsJsonArray);
        Assert.True(new JsonValue(default(JsonArray)).IsNull);
        Assert.False(new JsonValue(default(JsonArray)).IsJsonArray);
        Assert.Null(new JsonValue(default(JsonArray)).AsJsonArray);

        Assert.Equal("interface Microsoft.Extensions.Metrics.IHistogram2<in T> where T : struct",
            new JsonValue("interface Microsoft.Extensions.Metrics.IHistogram2<in T> where T : struct").AsString);
    }

    [Fact]
    public void TestIsInteger()
    {
        Assert.False(new JsonValue(false).IsInteger);
        Assert.True(new JsonValue(1.0).IsInteger);
        Assert.False(new JsonValue(1.1).IsInteger);
        Assert.False(new JsonValue(double.PositiveInfinity).IsInteger);
    }

    [Fact]
    public void TestIsDateTime()
    {
        Assert.False(new JsonValue(false).IsDateTime);
        Assert.False(new JsonValue("Some text").IsDateTime);
        Assert.True(new JsonValue(DateTime.Now.ToString("o", CultureInfo.InvariantCulture)).IsDateTime);
    }

    [Fact]
    public void TestAsBoolean()
    {
        Assert.False(new JsonValue(false).AsBoolean);
        Assert.False(new JsonValue(0.0).AsBoolean);
        Assert.False(new JsonValue(string.Empty).AsBoolean);
        Assert.False(new JsonValue(default(JsonObject)).AsBoolean);

        Assert.True(new JsonValue(true).AsBoolean);
        Assert.True(new JsonValue(1.0).AsBoolean);
        Assert.True(new JsonValue("text").AsBoolean);
        Assert.True(new JsonValue(new JsonObject()).AsBoolean);
        Assert.True(new JsonValue(new JsonArray()).AsBoolean);
    }

    [Fact]
    public void TestAsInteger()
    {
        Assert.Equal(int.MaxValue, new JsonValue(uint.MaxValue).AsInteger);
        Assert.Equal(int.MinValue, new JsonValue(long.MinValue).AsInteger);
        Assert.Equal(0, new JsonValue(0.5).AsInteger);
        Assert.Equal(1, new JsonValue(1).AsInteger);
    }

    [Fact]
    public void TestAsNumber()
    {
        Assert.Equal(0.0, new JsonValue(false).AsNumber);
        Assert.Equal(1.0, new JsonValue(true).AsNumber);
        Assert.Equal(1.0, new JsonValue(1.0).AsNumber);
        Assert.Equal(1.0, new JsonValue("1.0").AsNumber);
        Assert.Equal(0, new JsonValue("text").AsNumber);
        Assert.Equal(0.0, new JsonValue(new JsonObject()).AsNumber);
        Assert.Equal(0.0, new JsonValue(default(JsonObject)).AsNumber);
        Assert.Equal(0.0, new JsonValue(new JsonArray()).AsNumber);
    }

    [Fact]
    public void TestAsNumbersUsingCultureWithNonStandardNumberFormat()
    {
        Assert.Equal(1.0, new JsonValue("1.0").AsNumber);
    }

    [Fact]
    public void TestAsString()
    {
        Assert.Equal("false", new JsonValue(false).AsString);
        Assert.Equal("true", new JsonValue(true).AsString);
        Assert.Equal("0.5", new JsonValue(0.5).AsString);
        Assert.Equal("1", new JsonValue(1.0).AsString);
        Assert.Equal("text", new JsonValue("text").AsString);
        Assert.Null(new JsonValue(new JsonObject()).AsString);
        Assert.Null(new JsonValue(default(JsonObject)).AsString);
        Assert.Null(new JsonValue(new JsonArray()).AsString);
    }

    [Fact]
    public void TestAsStringUsingCultureWithNonStandardNumberFormat()
    {
        Assert.Equal("0.5", new JsonValue(0.5).AsString);
    }

    [Fact]
    public void TestAsJsonObject()
    {
        Assert.Null(new JsonValue(false).AsJsonObject);
        Assert.IsType<JsonObject>(new JsonValue(new JsonObject()).AsJsonObject);
        Assert.Null(new JsonValue(default(JsonObject)).AsJsonObject);
    }

    [Fact]
    public void TestAsJsonArray()
    {
        Assert.Null(new JsonValue(false).AsJsonArray);
        Assert.IsType<JsonArray>(new JsonValue(new JsonArray()).AsJsonArray);
        Assert.Null(new JsonValue(default(JsonArray)).AsJsonArray);
    }

    [Fact]
    public void TestAsDateTime()
    {
        Assert.Null(new JsonValue(false).AsDateTime);
        Assert.Null(new JsonValue("Some text").AsDateTime);

        var now = new DateTime(2016, 1, 20, 5, 12, 33, DateTimeKind.Local);
        Assert.Equal(now, new JsonValue(now.ToString("o", CultureInfo.InvariantCulture)).AsDateTime);
    }

    [Fact]
    public void TestAsObject()
    {
        Assert.Equal(0.0, new JsonValue(false).AsObject);
        Assert.Equal(1.0, new JsonValue(true).AsObject);
        Assert.Equal(1.0, new JsonValue(1.0).AsObject);
        Assert.Equal("1.0", new JsonValue("1.0").AsObject);
        Assert.IsType<JsonObject>(new JsonValue(new JsonObject()).AsObject);
        Assert.IsType<JsonArray>(new JsonValue(new JsonArray()).AsObject);
        Assert.Null(new JsonValue(default(JsonObject)).AsObject);
    }

    [Fact]
    public void TestStringIndexer()
    {
        Assert.ThrowsAny<InvalidOperationException>(() => new JsonValue(false)["key"]);
        Assert.ThrowsAny<InvalidOperationException>(() => new JsonValue(false)[null!]);
        Assert.ThrowsAny<InvalidOperationException>(() => new JsonValue(false) { ["key"] = "value" });

        Assert.Equal(JsonValue.Null, new JsonValue(new JsonObject())["key"]);
        var value = new JsonValue(new JsonObject()) { ["key"] = "value" };
        Assert.Equal("value", value["key"].AsString);
        Assert.ThrowsAny<ArgumentNullException>(() => new JsonValue(new JsonObject())[null!]);
    }

    [Fact]
    public void TestIntegerIndexer()
    {
        Assert.ThrowsAny<InvalidOperationException>(() => new JsonValue(false)[0]);
        Assert.ThrowsAny<InvalidOperationException>(() => new JsonValue(false)[-1]);
        Assert.ThrowsAny<InvalidOperationException>(() => new JsonValue(false) { [0] = "value" });

        Assert.Equal(JsonValue.Null, new JsonValue(new JsonArray())[0]);
        Assert.Equal(JsonValue.Null, new JsonValue(new JsonArray())[-1]);

        var value = new JsonValue(new JsonArray { "initial" });
        Assert.Equal("initial", value[0].AsString);
        value[0] = "value";
        Assert.Equal("value", value[0].AsString);
    }

    [Fact]
    public void TestConversionOperators()
    {
        // (JsonValue)(DateTime?)
        DateTime time = DateTime.Now;
        Assert.NotEqual(time, JsonValue.Null);
        Assert.Equal(time.ToString("o", CultureInfo.InvariantCulture), ((JsonValue)time).AsString);
        Assert.Equal(default(DateTime?), JsonValue.Null);

        // (int)(JsonValue)
        Assert.Equal(0, (int)new JsonValue(uint.MaxValue));
        Assert.Equal(0, (int)new JsonValue(long.MinValue));
        Assert.Equal(0, (int)new JsonValue(2.5));
        Assert.Equal(1, (int)new JsonValue(1));

        // (int?)(JsonValue)
        Assert.Equal(0, (int?)new JsonValue(uint.MaxValue));
        Assert.Equal(0, (int?)new JsonValue(long.MinValue));
        Assert.Equal(0, (int?)new JsonValue(2.5));
        Assert.Equal(1, (int?)new JsonValue(1));
        Assert.Null((int?)JsonValue.Null);
        Assert.Null((int?)new JsonValue(default(JsonObject)));

        // (bool)(JsonValue)
        Assert.False((bool)new JsonValue(false));
        Assert.False((bool)new JsonValue(0.0));
        Assert.False((bool)new JsonValue(string.Empty));
        Assert.False((bool)new JsonValue(default(JsonObject)));

        Assert.True((bool)new JsonValue(true));
        Assert.False((bool)new JsonValue(1.0));
        Assert.False((bool)new JsonValue("text"));
        Assert.False((bool)new JsonValue(new JsonObject()));
        Assert.False((bool)new JsonValue(new JsonArray()));

        // (bool?)(JsonValue)
        Assert.False((bool?)new JsonValue(false));
        Assert.False((bool?)new JsonValue(0.0));
        Assert.False((bool?)new JsonValue(string.Empty));

        Assert.True((bool?)new JsonValue(true));
        Assert.False((bool?)new JsonValue(1.0));
        Assert.False((bool?)new JsonValue("text"));
        Assert.False((bool?)new JsonValue(new JsonObject()));
        Assert.False((bool?)new JsonValue(new JsonArray()));

        Assert.Null((bool?)JsonValue.Null);
        Assert.Null((bool?)new JsonValue(default(JsonObject)));

        // (double)(JsonValue)
        Assert.Equal(double.NaN, (double)new JsonValue(false));
        Assert.Equal(double.NaN, (double)new JsonValue(true));
        Assert.Equal(1.0, (double)new JsonValue(1.0));
        Assert.Equal(double.NaN, (double)new JsonValue("1.0"));
        Assert.Equal(double.NaN, (double)new JsonValue(new JsonObject()));
        Assert.Equal(double.NaN, (double)new JsonValue(new JsonArray()));
        Assert.Equal(double.NaN, (double)JsonValue.Null);
        Assert.Equal(double.NaN, (double)new JsonValue(default(JsonObject)));

        // (double?)(JsonValue)
        Assert.Equal(double.NaN, (double?)new JsonValue(false));
        Assert.Equal(double.NaN, (double?)new JsonValue(true));
        Assert.Equal(1.0, (double?)new JsonValue(1.0));
        Assert.Equal(double.NaN, (double?)new JsonValue("1.0"));
        Assert.Equal(double.NaN, (double?)new JsonValue(new JsonObject()));
        Assert.Equal(double.NaN, (double?)new JsonValue(new JsonArray()));
        Assert.Null((double?)JsonValue.Null);
        Assert.Null((double?)new JsonValue(default(JsonObject)));

        // (string)(JsonValue)
        Assert.Null((string?)new JsonValue(false));
        Assert.Null((string?)new JsonValue(true));
        Assert.Null((string?)new JsonValue(1.0));
        Assert.Equal("text", (string?)new JsonValue("text"));
        Assert.Null((string?)new JsonValue(new JsonObject()));
        Assert.Null((string?)new JsonValue(default(JsonObject)));
        Assert.Null((string?)new JsonValue(new JsonArray()));

        // (JsonObject)(JsonValue)
        Assert.Null((JsonObject?)new JsonValue(false));
        Assert.IsType<JsonObject>((JsonObject?)new JsonValue(new JsonObject()));
        Assert.Null((JsonObject?)new JsonValue(default(JsonObject)));

        // (JsonArray)(JsonValue)
        Assert.Null((JsonArray?)new JsonValue(false));
        Assert.IsType<JsonArray>((JsonArray?)new JsonValue(new JsonArray()));
        Assert.Null((JsonArray?)new JsonValue(default(JsonArray)));

        // (DateTime)(JsonValue)
        Assert.Equal(DateTime.MinValue, (DateTime)new JsonValue(false));
        Assert.Equal(DateTime.MinValue, (DateTime)new JsonValue("Some text"));

        var now = new DateTime(2016, 1, 20, 5, 12, 33, DateTimeKind.Local);
        Assert.Equal(now, (DateTime)new JsonValue(now.ToString("o", CultureInfo.InvariantCulture)));

        // (DateTime?)(JsonValue)
        Assert.Null((DateTime?)new JsonValue(false));
        Assert.Null((DateTime?)new JsonValue("Some text"));
        Assert.Equal(now, (DateTime?)new JsonValue(now.ToString("o", CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void TestOpInequality()
    {
        Assert.False(JsonValue.Null != default);
        Assert.True(new JsonValue(true) != new JsonValue(0));
    }

    [Fact]
    public void TestEquals()
    {
        Assert.True(JsonValue.Null.Equals((object?)null));
        Assert.True(JsonValue.Null.Equals(JsonValue.Null));
        Assert.True(JsonValue.Null.Equals(default));

        Assert.True(new JsonValue(true).Equals(new JsonValue(true)));
        Assert.False(new JsonValue(true).Equals(new JsonValue(false)));

        Assert.False(JsonValue.Null.Equals(1));
        Assert.False(JsonValue.Null.Equals(new InvalidOperationException()));
    }

    [Fact]
    public void TestGetHashCode()
    {
        Assert.Equal(JsonValue.Null.GetHashCode(), default(JsonValue).GetHashCode());
        Assert.Equal(new JsonValue(1).GetHashCode(), new JsonValue(1).GetHashCode());
        Assert.Equal(new JsonValue("text").GetHashCode(), new JsonValue(new string("text".ToCharArray())).GetHashCode());
    }

    [Fact]
    public void TestParse()
    {
        Assert.True(JsonValue.Parse("true").IsBoolean);
        Assert.True(JsonValue.Parse("1").IsInteger);
        Assert.True(JsonValue.Parse("1.0").IsInteger);
        Assert.True(JsonValue.Parse("1.0").IsNumber);
        Assert.True(JsonValue.Parse("\"text\"").IsString);
        Assert.True(JsonValue.Parse("null").IsNull);
        Assert.True(JsonValue.Parse("{}").IsJsonObject);
        Assert.True(JsonValue.Parse("[]").IsJsonArray);

        Assert.ThrowsAny<JsonParseException>(() => JsonValue.Parse(string.Empty));
        Assert.ThrowsAny<JsonParseException>(() => JsonValue.Parse("{"));
    }
}
