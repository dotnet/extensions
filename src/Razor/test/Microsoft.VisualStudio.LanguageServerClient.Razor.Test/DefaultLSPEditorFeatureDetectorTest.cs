// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor
{
    public class DefaultLSPEditorFeatureDetectorTest
    {
        [Fact]
        public void IsLSPEditorAvailable_EnvironmentVariableTrue_ReturnsTrue()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                EnvironmentFeatureEnabledValue = true,
                ProjectSupportsRazorLSPEditorValue = true,
            };

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsLSPEditorAvailable_FeatureFlagEnabled_ReturnsTrue()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsFeatureFlagEnabledValue = true,
                ProjectSupportsRazorLSPEditorValue = true,
            };

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsLSPEditorAvailable_IsVSServer_ReturnsTrue()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsVSServerValue = true,
                ProjectSupportsRazorLSPEditorValue = true,
            };

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsLSPEditorAvailable_IsVSRemoteClient_ReturnsTrue()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsVSRemoteClientValue = true,
                ProjectSupportsRazorLSPEditorValue = true,
            };

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsLSPEditorAvailable_IsLiveShareHost_ReturnsFalse()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsLiveShareHostValue = true,
                ProjectSupportsRazorLSPEditorValue = true,
            };

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsLSPEditorAvailable_IsLiveShareGuest_ReturnsFalse()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsLiveShareGuestValue = true,
                ProjectSupportsRazorLSPEditorValue = true,
            };

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsLSPEditorAvailable_UnknownEnvironment_ReturnsFalse()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector();

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsLSPEditorAvailable_FeatureFlagEnabled_UnsupportedProject_ReturnsFalse()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsFeatureFlagEnabledValue = true,
                ProjectSupportsRazorLSPEditorValue = false,
            };

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsLSPEditorAvailable_FeatureFlagEnabled_SupportedProject_ReturnsTrue()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsFeatureFlagEnabledValue = true,
                ProjectSupportsRazorLSPEditorValue = true,
            };

            // Act
            var result = featureDetector.IsLSPEditorAvailable("testMoniker", hierarchy: null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRemoteClient_VSRemoteClient_ReturnsTrue()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsVSRemoteClientValue = true,
            };

            // Act
            var result = featureDetector.IsRemoteClient();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRemoteClient_LiveShareGuest_ReturnsTrue()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsLiveShareGuestValue = true,
            };

            // Act
            var result = featureDetector.IsRemoteClient();

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsRemoteClient_FeatureFlagEnabled_ReturnsFalse()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector()
            {
                IsFeatureFlagEnabledValue = true,
            };

            // Act
            var result = featureDetector.IsRemoteClient();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsRemoteClient_UnknownEnvironment_ReturnsFalse()
        {
            // Arrange
            var featureDetector = new TestLSPEditorFeatureDetector();

            // Act
            var result = featureDetector.IsRemoteClient();

            // Assert
            Assert.False(result);
        }

        private class TestLSPEditorFeatureDetector : DefaultLSPEditorFeatureDetector
        {
            public bool EnvironmentFeatureEnabledValue { get; set; }

            public bool IsFeatureFlagEnabledValue { get; set; }

            public bool IsLiveShareGuestValue { get; set; }

            public bool IsLiveShareHostValue { get; set; }

            public bool IsVSRemoteClientValue { get; set; }

            public bool IsVSServerValue { get; set; }

            public bool ProjectSupportsRazorLSPEditorValue { get; set; }

            private protected override bool EnvironmentFeatureEnabled() => EnvironmentFeatureEnabledValue;

            private protected override bool IsFeatureFlagEnabledCached() => IsFeatureFlagEnabledValue;

            private protected override bool IsLiveShareGuest() => IsLiveShareGuestValue;

            private protected override bool IsLiveShareHost() => IsLiveShareHostValue;

            private protected override bool IsVSRemoteClient() => IsVSRemoteClientValue;

            private protected override bool IsVSServer() => IsVSServerValue;

            private protected override bool ProjectSupportsRazorLSPEditor(string documentMoniker, IVsHierarchy hierarchy) => ProjectSupportsRazorLSPEditorValue;
        }
    }
}
