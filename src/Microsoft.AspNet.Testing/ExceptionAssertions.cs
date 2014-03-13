using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Testing
{
    // TODO: eventually want: public partial class Assert : Xunit.Assert
    public static class ExceptionAssert
    {
        /// <summary>
        /// Verifies that an exception of the given type is thrown.
        /// Also verifies that the exception message matches.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Action testCode, string exceptionMessage)
            where TException : Exception
        {
            var ex = Assert.Throws<TException>(testCode);
            VerifyExceptionMessage(ex, exceptionMessage);
            return ex;
        }

        /// <summary>
        /// Verifies that an exception of the given type is thrown.
        /// Also verifies that the exception message matches.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static async Task<TException> ThrowsAsync<TException>(Func<Task> testCode, string exceptionMessage)
            where TException : Exception
        {
            var ex = await Assert.ThrowsAsync<TException>(testCode);
            VerifyExceptionMessage(ex, exceptionMessage);
            return ex;
        }

        /// <summary>
        /// Verifies that an exception of the given type is thrown.
        /// Also verified that the exception message matches.
        /// </summary>
        /// <typeparam name="TException">The type of the exception expected to be thrown</typeparam>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static TException Throws<TException>(Func<object> testCode, string exceptionMessage)
            where TException : Exception
        {
            return Throws<TException>(() => { testCode(); }, exceptionMessage);
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgument(Action testCode, string paramName, string exceptionMessage)
        {
            var ex = Assert.Throws<ArgumentException>(paramName, testCode);
            VerifyExceptionMessage(ex, exceptionMessage, partialMatch: true);
            return ex;
        }

        /// <summary>
        /// Verifies that the code throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <param name="exceptionMessage">The exception message to verify</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static async Task<ArgumentException> ThrowsArgumentAsync(Func<Task> testCode, string paramName, string exceptionMessage)
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(paramName, testCode);
            VerifyExceptionMessage(ex, exceptionMessage, partialMatch: true);
            return ex;
        }

        /// <summary>
        /// Verifies that the code throws an ArgumentException with the expected message that indicates that the value cannot
        /// be null or empty.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgumentNullOrEmpty(Action testCode, string paramName)
        {
            return Throws<ArgumentException>(testCode, "Value cannot be null or empty.\r\nParameter name: " + paramName);
        }

        /// <summary>
        /// Verifies that the code throws an ArgumentException with the expected message that indicates that the value cannot
        /// be null or empty.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static Task<ArgumentException> ThrowsArgumentNullOrEmptyAsync(Func<Task> testCode, string paramName)
        {
            return ThrowsAsync<ArgumentException>(testCode, "Value cannot be null or empty.\r\nParameter name: " + paramName);
        }

        /// <summary>
        /// Verifies that the code throws an ArgumentNullException with the expected message that indicates that the value cannot
        /// be null or empty string.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static ArgumentException ThrowsArgumentNullOrEmptyString(Action testCode, string paramName)
        {
            return ThrowsArgument(testCode, paramName, "Value cannot be null or an empty string.");
        }

        /// <summary>
        /// Verifies that the code throws an ArgumentNullException with the expected message that indicates that the value cannot
        /// be null or empty string.
        /// </summary>
        /// <param name="testCode">A delegate to the code to be tested</param>
        /// <param name="paramName">The name of the parameter that should throw the exception</param>
        /// <returns>The exception that was thrown, when successful</returns>
        /// <exception>Thrown when an exception was not thrown, or when an exception of the incorrect type is thrown</exception>
        public static Task<ArgumentException> ThrowsArgumentNullOrEmptyStringAsync(Func<Task> testCode, string paramName)
        {
            return ThrowsArgumentAsync(testCode, paramName, "Value cannot be null or an empty string.");
        }

        private static void VerifyException(Type exceptionType, Exception exception)
        {
            Assert.NotNull(exception);
            Assert.IsAssignableFrom(exceptionType, exception);
        }

        private static void VerifyExceptionMessage(Exception exception, string expectedMessage,
            bool partialMatch = false)
        {
            if (expectedMessage != null)
            {
                if (!partialMatch)
                {
                    Assert.Equal(expectedMessage, exception.Message);
                }
                else
                {
                    Assert.Contains(expectedMessage, exception.Message);
                }
            }
        }
    }
}