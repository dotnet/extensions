// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Framework.Notification
{
    public class NotifierTest
    {
        public class OneTarget
        {
            public int OneCallCount { get; private set; }

            [NotificationName("One")]
            public void One()
            {
                ++OneCallCount;
            }
        }

        [Fact]
        public void ShouldNotifyBecomesTrueAfterEnlisting()
        {
            var notifier = CreateNotifier();

            Assert.False(notifier.ShouldNotify("One"));
            Assert.False(notifier.ShouldNotify("Two"));

            notifier.EnlistTarget(new OneTarget());

            Assert.True(notifier.ShouldNotify("One"));
            Assert.False(notifier.ShouldNotify("Two"));
        }

        [Fact]
        public void CallingNotifyWillInvokeMethod()
        {
            var notifier = CreateNotifier();
            var target = new OneTarget();

            notifier.EnlistTarget(target);

            Assert.Equal(0, target.OneCallCount);
            notifier.Notify("One", new { });
            Assert.Equal(1, target.OneCallCount);
        }

        [Fact]
        public void CallingNotifyForNonEnlistedNameIsHarmless()
        {
            var notifier = CreateNotifier();
            var target = new OneTarget();

            notifier.EnlistTarget(target);

            Assert.Equal(0, target.OneCallCount);
            notifier.Notify("Two", new { });
            Assert.Equal(0, target.OneCallCount);
        }

        private class TwoTarget
        {
            public string Alpha { get; private set; }
            public string Beta { get; private set; }
            public int Delta { get; private set; }

            [NotificationName("Two")]
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
            var notifier = CreateNotifier();
            var target = new TwoTarget();

            notifier.EnlistTarget(target);

            notifier.Notify("Two", new { alpha = "ALPHA", beta = "BETA", delta = -1 });

            Assert.Equal("ALPHA", target.Alpha);
            Assert.Equal("BETA", target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void ExtraParametersAreHarmless()
        {
            var notifier = CreateNotifier();
            var target = new TwoTarget();

            notifier.EnlistTarget(target);

            notifier.Notify("Two", new { alpha = "ALPHA", beta = "BETA", delta = -1, extra = this });

            Assert.Equal("ALPHA", target.Alpha);
            Assert.Equal("BETA", target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void MissingParametersArriveAsNull()
        {
            var notifier = CreateNotifier();
            var target = new TwoTarget();

            notifier.EnlistTarget(target);
            notifier.Notify("Two", new { alpha = "ALPHA", delta = -1 });

            Assert.Equal("ALPHA", target.Alpha);
            Assert.Null(target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void NotificationCanDuckType()
        {
            var notifier = CreateNotifier();
            var target = new ThreeTarget();

            notifier.EnlistTarget(target);
            notifier.Notify("Three", new
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

            Assert.Equal("Alpha", target.Person.FirstName);
            Assert.Equal("Beta", target.Person.Address.City);
            Assert.Equal("Gamma", target.Person.Address.State);
            Assert.Equal(98028, target.Person.Address.Zip);
        }

        public class ThreeTarget
        {
            public IPerson Person { get; private set; }

            [NotificationName("Three")]
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

        private static INotifier CreateNotifier()
        {
            return new Notifier(new ProxyNotifierMethodAdapter());
        }
    }
}
