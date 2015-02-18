// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Xunit;

namespace Microsoft.Framework.Internal
{
    public class PropertyActivatorTest
    {
        [Fact]
        public void Activate_InvokesValueAccessorWithExpectedValue()
        {
            // Arrange
            var instance = new TestClass();
            var typeInfo = instance.GetType().GetTypeInfo();
            var property = typeInfo.GetDeclaredProperty("IntProperty");
            var invokedWith = -1;
            var activator = new PropertyActivator<int>(
                property,
                valueAccessor: (val) =>
                {
                    invokedWith = val;
                    return val;
                });

            // Act
            activator.Activate(instance, 123);

            // Assert
            Assert.Equal(123, invokedWith);
        }

        [Fact]
        public void Activate_SetsPropertyValue()
        {
            // Arrange
            var instance = new TestClass();
            var typeInfo = instance.GetType().GetTypeInfo();
            var property = typeInfo.GetDeclaredProperty("IntProperty");
            var activator = new PropertyActivator<int>(property, valueAccessor: (val) => val + 1);

            // Act
            activator.Activate(instance, 123);

            // Assert
            Assert.Equal(124, instance.IntProperty);
        }

        [Fact]
        public void GetPropertiesToActivate_RestrictsActivatableProperties()
        {
            // Arrange
            var instance = new TestClass();
            var typeInfo = instance.GetType().GetTypeInfo();
            var expectedPropertyInfo = typeInfo.GetDeclaredProperty("ActivatableProperty");

            // Act
            var propertiesToActivate = PropertyActivator<int>.GetPropertiesToActivate(
                type: typeof(TestClass),
                activateAttributeType: typeof(TestActivateAttribute),
                createActivateInfo:
                (propertyInfo) => new PropertyActivator<int>(propertyInfo, valueAccessor: (val) => val + 1));

            // Assert
            Assert.Collection(
                propertiesToActivate,
                (activator) =>
                {
                    Assert.Equal(expectedPropertyInfo, activator.PropertyInfo);
                });
        }

        [Fact]
        public void GetPropertiesToActivate_CanCreateCustomPropertyActivators()
        {
            // Arrange
            var instance = new TestClass();
            var typeInfo = instance.GetType().GetTypeInfo();
            var expectedPropertyInfo = typeInfo.GetDeclaredProperty("IntProperty");

            // Act
            var propertiesToActivate = PropertyActivator<int>.GetPropertiesToActivate(
                type: typeof(TestClass),
                activateAttributeType: typeof(TestActivateAttribute),
                createActivateInfo:
                (propertyInfo) => new PropertyActivator<int>(expectedPropertyInfo, valueAccessor: (val) => val + 1));

            // Assert
            Assert.Collection(
                propertiesToActivate,
                (activator) =>
                {
                    Assert.Equal(expectedPropertyInfo, activator.PropertyInfo);
                });
        }

        private class TestClass
        {
            public int IntProperty { get; set; }

            [TestActivate]
            public int ActivatableProperty { get; set; }

            [TestActivate]
            public int NoSetterActivatableProperty { get; }

            [TestActivate]
            public int this[int something] // Not activatable
            {
                get
                {
                    return 0;
                }
            }

            [TestActivate]
            public static int StaticActivatablProperty { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property)]
        private class TestActivateAttribute : Attribute
        {
        }

        private class ActivationInfo
        {
            public string Name { get; set; }
        }
    }
}
