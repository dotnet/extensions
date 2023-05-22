// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options.Contextual;
using TestClasses;
using Xunit;

namespace Microsoft.Gen.ContextualOptions.Test;

[SuppressMessage("Style", "IDE0004:Remove Unnecessary Cast", Justification = "The tests fail without the cast.")]
public class ContextualOptionsTests
{
    private class Receiver : IOptionsContextReceiver
    {
        public List<(string key, object? value)> Received { get; } = new();

        public void Receive<T>(string key, T value) => Received.Add((key, value));
    }

    [Fact]
    public void Class()
    {
        Receiver receiver = new();
        ((IOptionsContext)new Class1()).PopulateReceiver(receiver);
        Assert.Single(receiver.Received, ("Foo", (object?)"FooValue"));
    }

    [Fact]
    public void TwoPartClass()
    {
        Receiver receiver = new();
        ((IOptionsContext)new Class2()).PopulateReceiver(receiver);
        Assert.Equal(2, receiver.Received.Count);
        Assert.Contains(("Foo", (object?)"FooValue"), receiver.Received);
        Assert.Contains(("Bar", (object?)"BarValue"), receiver.Received);
    }

    [Fact]
    public void Record()
    {
        Receiver receiver = new();
        ((IOptionsContext)new Record1("PropertyValue")).PopulateReceiver(receiver);
        Assert.Single(receiver.Received, ("Foo", (object?)"PropertyValue"));
    }

    [Fact]
    public void Struct()
    {
        Receiver receiver = new();
        ((IOptionsContext)default(Struct1)).PopulateReceiver(receiver);
        Assert.Single(receiver.Received, ("Foo", (object?)"FooValue"));
    }

    [Fact]
    public void NonPublicType()
    {
        Assert.False(typeof(NonPublicStruct).IsPublic);
        Assert.IsAssignableFrom<IOptionsContext>(default(NonPublicStruct));
    }
}
