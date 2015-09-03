// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Framework.TelemetryAdapter
{
    public class DefaultTelemetrySourceAdapterTest
    {
        public class OneTarget
        {
            public int OneCallCount { get; private set; }

            [TelemetryName("One")]
            public void One()
            {
                ++OneCallCount;
            }
        }

        [Fact]
        public void IsEnabledBecomesTrueAfterEnlisting()
        {
            // Arrange
            var adapter = CreateAdapter();

            // Act & Assert
            Assert.False(adapter.IsEnabled("One"));
            Assert.False(adapter.IsEnabled("Two"));

            adapter.EnlistTarget(new OneTarget());

            Assert.True(adapter.IsEnabled("One"));
            Assert.False(adapter.IsEnabled("Two"));
        }

        [Fact]
        public void CallingWriteTelemetryWillInvokeMethod()
        {
            // Arrange
            var adapter = CreateAdapter();
            var target = new OneTarget();

            adapter.EnlistTarget(target);

            // Act & Assert
            Assert.Equal(0, target.OneCallCount);
            adapter.WriteTelemetry("One", new { });
            Assert.Equal(1, target.OneCallCount);
        }

        [Fact]
        public void CallingWriteTelemetryForNonEnlistedNameIsHarmless()
        {
            // Arrange
            var adapter = CreateAdapter();
            var target = new OneTarget();

            adapter.EnlistTarget(target);

            // Act & Assert
            Assert.Equal(0, target.OneCallCount);
            adapter.WriteTelemetry("Two", new { });
            Assert.Equal(0, target.OneCallCount);
        }

        private class TwoTarget
        {
            public string Alpha { get; private set; }
            public string Beta { get; private set; }
            public int Delta { get; private set; }

            [TelemetryName("Two")]
            public void Two(string alpha, string beta, int delta)
            {
                Alpha = alpha;
                Beta = beta;
                Delta = delta;
            }
        }

        [Fact]
        public void ParametersWillSplatFromObjectByName()
        {
            // Arrange
            var adapter = CreateAdapter();
            var target = new TwoTarget();

            adapter.EnlistTarget(target);

            // Act
            adapter.WriteTelemetry("Two", new { alpha = "ALPHA", beta = "BETA", delta = -1 });

            // Assert
            Assert.Equal("ALPHA", target.Alpha);
            Assert.Equal("BETA", target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void ExtraParametersAreHarmless()
        {
            // Arrange
            var adapter = CreateAdapter();
            var target = new TwoTarget();

            adapter.EnlistTarget(target);

            // Act
            adapter.WriteTelemetry("Two", new { alpha = "ALPHA", beta = "BETA", delta = -1, extra = this });

            // Assert
            Assert.Equal("ALPHA", target.Alpha);
            Assert.Equal("BETA", target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void MissingParametersArriveAsNull()
        {
            // Arrange
            var adapter = CreateAdapter();
            var target = new TwoTarget();

            adapter.EnlistTarget(target);

            // Act
            adapter.WriteTelemetry("Two", new { alpha = "ALPHA", delta = -1 });

            // Assert
            Assert.Equal("ALPHA", target.Alpha);
            Assert.Null(target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void WriteTelemetryCanDuckType()
        {
            // Arrange
            var adapter = CreateAdapter();
            var target = new ThreeTarget();

            adapter.EnlistTarget(target);

            // Act
            adapter.WriteTelemetry("Three", new
            {
                person = new Person
                {
                    FirstName = "Alpha",
                    Address = new Address
                    {
                        City = "Beta",
                        State = "Gamma",
                        Zip = 98028
                    }
                }
            });

            // Assert
            Assert.Equal("Alpha", target.Person.FirstName);
            Assert.Equal("Beta", target.Person.Address.City);
            Assert.Equal("Gamma", target.Person.Address.State);
            Assert.Equal(98028, target.Person.Address.Zip);
        }

        public class ThreeTarget
        {
            public IPerson Person { get; private set; }

            [TelemetryName("Three")]
            public void Three(IPerson person)
            {
                Person = person;
            }
        }

        public interface IPerson
        {
            string FirstName { get; }
            string LastName { get; }
            IAddress Address { get; }
        }

        public interface IAddress
        {
            string City { get; }
            string State { get; }
            int Zip { get; }
        }

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Address Address { get; set; }
        }

        public class DerivedPerson : Person
        {
            public double CoolnessFactor { get; set; }
        }

        public class Address
        {
            public string City { get; set; }
            public string State { get; set; }
            public int Zip { get; set; }
        }

        public class SomeValueType
        {
            public SomeValueType(int value)
            {
                Value = value;
            }

            public int Value { get; private set; }
        }

        private static TelemetrySourceAdapter CreateAdapter()
        {
            return new DefaultTelemetrySourceAdapter(new ProxyTelemetrySourceMethodAdapter());
        }
    }
}
