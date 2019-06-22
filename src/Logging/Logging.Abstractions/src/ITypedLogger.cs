// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// An extended logger that is capable of efficient logging of strongly-typed values, passed directly without wrapping them in an object array.
    /// </summary>
    public interface ITypedLogger
    {
        /// <summary>
        /// Logs a predefined string.
        /// </summary>
        /// <param name="logEntry">The original direct entry to be logged.</param>
        void OnFormatted(string logEntry);

        /// <summary>
        /// Logs the original format with a single value.
        /// </summary>
        /// <typeparam name="T0">The type of the value.</typeparam>
        /// <param name="originalFormat">The original value.</param>
        /// <param name="value0">The value of type <typeparamref name="T0"/>.</param>
        void OnFormatted<T0>(string originalFormat, T0 value0);

        /// <summary>
        /// Logs the specified format with a two values.
        /// </summary>
        /// <typeparam name="T0">The type of the first value.</typeparam>
        /// <typeparam name="T1">The type of the second value.</typeparam>
        /// <param name="originalFormat">The original value.</param>
        /// <param name="value0">The value of type <typeparamref name="T0"/>.</param>
        /// <param name="value1">The value of type <typeparamref name="T1"/>.</param>
        void OnFormatted<T0, T1>(string originalFormat, T0 value0, T1 value1);

        /// <summary>
        /// Logs the specified format with three values.
        /// </summary>
        /// <typeparam name="T0">The type of the first value.</typeparam>
        /// <typeparam name="T1">The type of the second value.</typeparam>
        /// <typeparam name="T2">The type of the third value.</typeparam>
        /// <param name="originalFormat">The original value.</param>
        /// <param name="value0">The value of type <typeparamref name="T0"/>.</param>
        /// <param name="value1">The value of type <typeparamref name="T1"/>.</param>
        /// <param name="value2">The value of type <typeparamref name="T2"/>.</param>
        void OnFormatted<T0, T1, T2>(string originalFormat, T0 value0, T1 value1, T2 value2);

        /// <summary>
        /// Logs the specified format with four values.
        /// </summary>
        /// <typeparam name="T0">The type of the first value.</typeparam>
        /// <typeparam name="T1">The type of the second value.</typeparam>
        /// <typeparam name="T2">The type of the third value.</typeparam>
        /// <typeparam name="T3">The type of the fourth value.</typeparam>
        /// <param name="originalFormat">The original value.</param>
        /// <param name="value0">The value of type <typeparamref name="T0"/>.</param>
        /// <param name="value1">The value of type <typeparamref name="T1"/>.</param>
        /// <param name="value2">The value of type <typeparamref name="T2"/>.</param>
        /// <param name="value3">The value of type <typeparamref name="T3"/>.</param>
        void OnFormatted<T0, T1, T2, T3>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3);

        /// <summary>
        /// Logs the specified format with five values.
        /// </summary>
        /// <typeparam name="T0">The type of the first value.</typeparam>
        /// <typeparam name="T1">The type of the second value.</typeparam>
        /// <typeparam name="T2">The type of the third value.</typeparam>
        /// <typeparam name="T3">The type of the fourth value.</typeparam>
        /// <typeparam name="T4">The type of the fifth value.</typeparam>
        /// <param name="originalFormat">The original value.</param>
        /// <param name="value0">The value of type <typeparamref name="T0"/>.</param>
        /// <param name="value1">The value of type <typeparamref name="T1"/>.</param>
        /// <param name="value2">The value of type <typeparamref name="T2"/>.</param>
        /// <param name="value3">The value of type <typeparamref name="T3"/>.</param>
        /// <param name="value4">The value of type <typeparamref name="T4"/>.</param>
        void OnFormatted<T0, T1, T2, T3, T4>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4);

        /// <summary>
        /// Logs the specified format with six values.
        /// </summary>
        /// <typeparam name="T0">The type of the first value.</typeparam>
        /// <typeparam name="T1">The type of the second value.</typeparam>
        /// <typeparam name="T2">The type of the third value.</typeparam>
        /// <typeparam name="T3">The type of the fourth value.</typeparam>
        /// <typeparam name="T4">The type of the fifth value.</typeparam>
        /// <typeparam name="T5">The type of the sixth value.</typeparam>
        /// <param name="originalFormat">The original value.</param>
        /// <param name="value0">The value of type <typeparamref name="T0"/>.</param>
        /// <param name="value1">The value of type <typeparamref name="T1"/>.</param>
        /// <param name="value2">The value of type <typeparamref name="T2"/>.</param>
        /// <param name="value3">The value of type <typeparamref name="T3"/>.</param>
        /// <param name="value4">The value of type <typeparamref name="T4"/>.</param>
        /// <param name="value5">The value of type <typeparamref name="T5"/>.</param>
        void OnFormatted<T0, T1, T2, T3, T4, T5>(string originalFormat, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5);
    }
}
