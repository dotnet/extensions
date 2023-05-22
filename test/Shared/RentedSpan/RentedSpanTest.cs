// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Shared.Pools.Test;

public static class RentedSpanTest
{
    [Fact]
    public static void Basic()
    {
        using var rental1 = new RentedSpan<int>(0);
        Assert.False(rental1.Rented);
        Assert.Equal(0, rental1.Span.Length);

        using var rental2 = new RentedSpan<int>(1);
        Assert.False(rental2.Rented);
        Assert.Equal(0, rental2.Span.Length);

        using var rental3 = new RentedSpan<byte>(RentedSpan<byte>.MinimumRentalSpace - 1);
        Assert.False(rental3.Rented);
        Assert.Equal(0, rental3.Span.Length);

        using var rental4 = new RentedSpan<byte>(RentedSpan<byte>.MinimumRentalSpace);
        Assert.True(rental4.Rented);
        Assert.Equal(RentedSpan<byte>.MinimumRentalSpace, rental4.Span.Length);

        using var rental5 = new RentedSpan<byte>(RentedSpan<byte>.MinimumRentalSpace + 1);
        Assert.True(rental5.Rented);
        Assert.Equal(RentedSpan<byte>.MinimumRentalSpace + 1, rental5.Span.Length);
    }
}
