// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Test.MessageInterception
{
    public class DefaultInterceptionManagerTest
    {
        [Fact]
        public void Ctor_NullArguments_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new DefaultInterceptorManager(null!));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void HasInterceptor_InvalidMessageName_Throws(string input)
        {
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors());

            Assert.Throws<ArgumentException>(() => sut.HasInterceptor(input));
        }

        [Fact]
        public void HasInterceptor_HasNoInterceptors_ReturnsFalse()
        {
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors());

            Assert.False(sut.HasInterceptor("foo"));
        }

        [Fact]
        public void HasInterceptor_HasMatchingInterceptor_ReturnsTrue()
        {
            var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "expected")));

            Assert.True(sut.HasInterceptor("expected"));
        }

        [Fact]
        public void HasInterceptor_DoesNotHaveMatchingInterceptor_ReturnsFalse()
        {
            var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "unexpected")));

            Assert.False(sut.HasInterceptor("expected"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ProcessInterceptorsAsync_InvalidMethodName_Throws(string input)
        {
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors());

            await Assert.ThrowsAsync<ArgumentException>(async () => await sut.ProcessInterceptorsAsync(input, JToken.Parse("{}"), "valid", CancellationToken.None));
        }

        [Fact]
        public async Task ProcessInterceptorsAsync_InvalidMessage_Throws()
        {
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors());

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await sut.ProcessInterceptorsAsync("valid", null!, "valid", CancellationToken.None));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task ProcessInterceptorsAsync_InvalidSourceLanguageName_Throws(string input)
        {
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors());

            await Assert.ThrowsAsync<ArgumentException>(async () => await sut.ProcessInterceptorsAsync("valid", JToken.Parse("{}"), input, CancellationToken.None));
        }

        [Fact]
        public async Task ProcessInterceptorsAsync_NoInterceptorMatches_NoChangesMadeToToken()
        {
            var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "unexpected")));
            var testToken = JToken.Parse("\"theToken\"");

            var result = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testLangauge", CancellationToken.None);

            Assert.Equal(testToken, result);
        }

        [Fact]
        public async Task ProcessInterceptorsAsync_InterceptorMatchesButDoesNotChangeDocumentUri_ChangesAppliedToToken()
        {
            var expected = JToken.Parse("\"new token\"");
            var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
            Mock.Get(fakeInterceptor).Setup(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                     .Returns(Task.FromResult(new InterceptionResult(expected, false)));
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "testMessage")));
            var testToken = JToken.Parse("\"theToken\"");

            var result = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testLangauge", CancellationToken.None);

            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task ProcessInterceptorsAsync_InterceptorMatches_ChangedTokenPassedToSecondInterceptor()
        {
            var expected = JToken.Parse("\"new token\"");
            var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
            Mock.Get(fakeInterceptor).Setup(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                     .Returns(Task.FromResult(new InterceptionResult(expected, false)));
            var mockSecondInterceptor = new Mock<MessageInterceptor>(MockBehavior.Strict);
            mockSecondInterceptor.Setup(x => x.ApplyChangesAsync(new JArray(), "testLangauge", CancellationToken.None))
                .Returns(Task.FromResult(new InterceptionResult(expected, false)));
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "testMessage"),
                                                                             (mockSecondInterceptor.Object, "testMessage")));
            var testToken = JToken.Parse("\"theToken\"");

            _ = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testLangauge", CancellationToken.None);

            mockSecondInterceptor.Verify(x => x.ApplyChangesAsync(It.Is<JToken>(t => t.Equals(expected)), It.IsAny<string>(), It.IsAny<CancellationToken>()));
            mockSecondInterceptor.Verify(x => x.ApplyChangesAsync(It.Is<JToken>(t => t.Equals(testToken)), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                                         Times.Never);
        }

        [Fact]
        public async Task ProcessInterceptorsAsync_InterceptorChangesDocumentUri_CausesAdditionalPass()
        {
            var expected = JToken.Parse("\"new token\"");
            var mockInterceptor =new Mock<MessageInterceptor>(MockBehavior.Strict);
            mockInterceptor.SetupSequence(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                           .Returns(Task.FromResult(new InterceptionResult(expected, true)))
                           .Returns(Task.FromResult(new InterceptionResult(expected, false)));
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((mockInterceptor.Object, "testMessage")));
            var testToken = JToken.Parse("\"theToken\"");

            _ = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testLangauge", CancellationToken.None);

            mockInterceptor.Verify(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                                   Times.Exactly(2));
        }

        [Fact]
        public async Task ProcessInterceptorsAsync_InterceptorReturnsNull_DoesNotCallAdditionalInterceptors()
        {
            var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
            Mock.Get(fakeInterceptor).Setup(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                     .Returns(Task.FromResult(new InterceptionResult(null, false)));
            var mockSecondInterceptor = new Mock<MessageInterceptor>(MockBehavior.Strict);
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "testMessage"),
                                                                             (mockSecondInterceptor.Object, "testMessage")));
            var testToken = JToken.Parse("\"theToken\"");

            _ = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testLangauge", CancellationToken.None);

            mockSecondInterceptor.Verify(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                                        Times.Never);
        }

        [Fact]
        public async Task ProcessInterceptorsAsync_InterceptorReturnsNull_ReturnsNull()
        {
            var fakeInterceptor = Mock.Of<MessageInterceptor>(MockBehavior.Strict);
            Mock.Get(fakeInterceptor).Setup(x => x.ApplyChangesAsync(It.IsAny<JToken>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                     .Returns(Task.FromResult(new InterceptionResult(null, false)));
            var sut = new DefaultInterceptorManager(GenerateLazyInterceptors((fakeInterceptor, "testMessage")));
            var testToken = JToken.Parse("\"theToken\"");

            var result = await sut.ProcessInterceptorsAsync("testMessage", testToken, "testLangauge", CancellationToken.None);

            Assert.Null(result);
        }

        private IEnumerable<Lazy<MessageInterceptor, IInterceptMethodMetadata>> GenerateLazyInterceptors(params (MessageInterceptor, string)[] fakeInterceptors)
        {
            var result = new List<Lazy<MessageInterceptor, IInterceptMethodMetadata>>();

            foreach ((var i, var metadataString) in fakeInterceptors)
            {
                var metadata = Mock.Of<IInterceptMethodMetadata>(MockBehavior.Strict);
                Mock.Get(metadata).Setup(m => m.InterceptMethods).Returns(new string[] { metadataString });
                result.Add(new Lazy<MessageInterceptor, IInterceptMethodMetadata>(() => i, metadata));
            }

            return result;
        }
    }
}
