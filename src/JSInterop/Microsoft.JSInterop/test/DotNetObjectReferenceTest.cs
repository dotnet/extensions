// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.JSInterop
{
    public class DotNetObjectReferenceTest
    {
        [Fact]
        public void CanAccessValue()
        {
            var obj = new object();
            Assert.Same(obj, DotNetObjectReference.Create(obj).Value);
        }

        [Fact]
        public void TrackObjectReference_AssignsObjectId()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var objRef = DotNetObjectReference.Create(new object());

            // Act
            var objectId = objRef.TrackObjectReference(jsRuntime);

            // Act
            Assert.Equal(objectId, objRef.ObjectId);
            Assert.Equal(1, objRef.ObjectId);
        }

        [Fact]
        public void TrackObjectReference_AllowsMultipleCallsUsingTheSameJSRuntime()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var objRef = DotNetObjectReference.Create(new object());

            // Act
            var objectId1 = objRef.TrackObjectReference(jsRuntime);
            var objectId2 = objRef.TrackObjectReference(jsRuntime);

            // Act
            Assert.Equal(objectId1, objectId2);
        }

        [Fact]
        public void TrackObjectReference_ThrowsIfDifferentJSRuntimeInstancesAreUsed()
        {
            // Arrange
            var objRef = DotNetObjectReference.Create("Hello world");
            var expected = $"{objRef.GetType().Name} is already being tracked by a different instance of {nameof(JSRuntime)}. A common cause is caching an instance of {nameof(DotNetObjectReference<string>)}" +
                    $" globally. Consider creating instances of {nameof(DotNetObjectReference<string>)} at the JSInterop callsite instead";
            var jsRuntime1 = new TestJSRuntime();
            var jsRuntime2 = new TestJSRuntime();
            objRef.TrackObjectReference(jsRuntime1);

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => objRef.TrackObjectReference(jsRuntime2));

            // Assert
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void Dispose_StopsTrackingObject()
        {
            // Arrange
            var objRef = DotNetObjectReference.Create("Hello world");
            var jsRuntime = new TestJSRuntime();
            objRef.TrackObjectReference(jsRuntime);
            var objectId = objRef.ObjectId;
            var expected = $"There is no tracked object with id '{objectId}'. Perhaps the DotNetObjectReference instance was already disposed.";

            // Act
            Assert.Same(objRef, jsRuntime.GetObjectReference(objectId));
            objRef.Dispose();

            // Assert
            Assert.True(objRef.Disposed);
            Assert.Throws<ArgumentException>(() => jsRuntime.GetObjectReference(objectId));
        }

        [Fact]
        public void DoubleDispose_Works()
        {
            // Arrange
            var objRef = DotNetObjectReference.Create("Hello world");
            var jsRuntime = new TestJSRuntime();
            objRef.TrackObjectReference(jsRuntime);
            var objectId = objRef.ObjectId;
            var expected = $"There is no tracked object with id '{objectId}'. Perhaps the DotNetObjectReference instance was already disposed.";

            // Act
            Assert.Same(objRef, jsRuntime.GetObjectReference(objectId));
            objRef.Dispose();

            // Assert
            objRef.Dispose();
            // If we got this far, this did not throw.
        }
    }
}
