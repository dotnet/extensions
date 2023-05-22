// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.AsyncState;

/// <summary>
/// We use this generic type to store values into <see cref="AspNetCore.Http.HttpContext.Features"/>.
/// Instead of storing the value under it's type T, we use the type <see cref="TypeWrapper{T}"/>.
/// Even if T is publicly available type and another value was stored into
/// <see cref="AspNetCore.Http.HttpContext.Features"/> under type T (by the application or another library),
/// this other value will not conflict with the one stored under <see cref="TypeWrapper{T}"/>.
/// Note that <see cref="TypeWrapper{T}"/> is not public, so nobody else can use it.
/// </summary>
/// <typeparam name="T">The type of the value to store into <see cref="AspNetCore.Http.HttpContext.Features"/>.</typeparam>
#pragma warning disable S1694  // Convert this 'abstract' class to a concrete type with protected constructor.
internal abstract class TypeWrapper<T>
#pragma warning restore S1694  // Convert this 'abstract' class to a concrete type with protected constructor.
{
}
