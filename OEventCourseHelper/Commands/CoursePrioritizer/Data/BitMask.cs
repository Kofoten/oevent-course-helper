using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal readonly record struct BitMask : IEquatable<BitMask>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitMask"/>.
    /// </summary>
    /// <param name="buckets">The buckets of the <see cref="BitMask"/>.</param>
    public BitMask(ImmutableArray<ulong> buckets)
    {
        Buckets = buckets;
        IsZero = BitOps.IsZero(this);
    }

    /// <summary>
    /// The underlying array of buckets.
    /// </summary>
    public readonly ImmutableArray<ulong> Buckets { get; private init; }

    /// <summary>
    /// Indicates if all bits in the <see cref="BitMask"/> is set to zero.
    /// </summary>
    public bool IsZero { get; private init; }

    public bool this[int bitIndex] => BitOps.IsSet(this, bitIndex);

    /// <summary>
    /// The length of the underlying bucket array.
    /// </summary>
    public int BucketCount => Buckets.Length;

    /// <summary>
    /// Creates an enumerator looping through the indicies of all set bits in the <see cref="BitMask"/>.
    /// </summary>
    /// <returns>A new <see cref="BitMaskEnumerator"/>.</returns>
    public BitMaskEnumerator GetEnumerator() => new(this);

    public static implicit operator ReadOnlySpan<ulong>(BitMask mask) => mask.Buckets.AsSpan();

    /// <summary>
    /// Creates a new <see cref="BitMask"/> from this instance where the specified bit is set.
    /// </summary>
    /// <param name="bitIndex">The index of the bit to set.</param>
    /// <returns>A new <see cref="BitMask"/>.</returns>
    /// <exception cref="IndexOutOfRangeException">If <paramref name="bitIndex"/> is outside the bounds of the <see cref="BitMask"/>.</exception>
    public BitMask Set(int bitIndex)
    {
        var bucketIndex = BitOps.GetBucketIndex(bitIndex);
        BitOps.ThrowIfOutOfBounds(bucketIndex, this);

        var builder = Builder.From(this);
        builder.Set(bitIndex);
        return builder.ToBitMask();
    }

    /// <summary>
    /// Creates a new <see cref="BitMask"/> containing the result of the AND NOT operation between this instance and <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The <see cref="BitMask"/> to use for the operation.</param>
    /// <returns>A new <see cref="BitMask"/>.</returns>
    /// <exception cref="InvalidOperationException">If the length of this <see cref="BitMask"/> and <paramref name="other"/> differs.</exception>
    public BitMask AndNot(BitMask other)
    {
        BitOps.ThrowIfDifferentLength(this, other, nameof(AndNot));

        var builder = Builder.From(this);
        builder.AndNot(other);
        return builder.ToBitMask();
    }

    /// <summary>
    /// Calculates if this instance is a subset of <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The <see cref="BitMask"/> to check against.</param>
    /// <returns>True if this instance is a subset of <paramref name="other"/>; otherwise False.</returns>
    /// <exception cref="InvalidOperationException">If the length of this <see cref="BitMask"/> and <paramref name="other"/> differs.</exception>
    public bool IsSubsetOf(BitMask other)
    {
        BitOps.ThrowIfDifferentLength(this, other, nameof(IsSubsetOf));

        return BitOps.IsSubsetOf(this, other);
    }

    /// <summary>
    /// Calculates the number of buckets required to hold the specified amount of bits.
    /// </summary>
    /// <param name="bitCount">The number of bits to be stored.</param>
    /// <returns>The number of required buckets.</returns>
    public static int GetBucketCount(int bitCount) => BitOps.GetBucketCount(bitCount);

    #region Factories
    /// <summary>
    /// Creates a new <see cref="BitMask"/> containing only set bits.
    /// </summary>
    /// <param name="bitCount">The number of bits in the <see cref="BitMask"/>.</param>
    /// <returns>A new <see cref="BitMask"/>.</returns>
    public static BitMask Fill(int bitCount)
    {
        var bucketCount = BitOps.GetBucketCount(bitCount);
        var mask = new ulong[bucketCount];

        for (int i = 0; i < bucketCount - 1; i++)
        {
            mask[i] = ulong.MaxValue;
        }

        var remainder = bitCount & 63;
        if (remainder == 0)
        {
            mask[^1] = ulong.MaxValue;
        }
        else
        {
            mask[^1] = (1UL << remainder) - 1;
        }

        var immutable = ImmutableCollectionsMarshal.AsImmutableArray(mask);
        return new BitMask(immutable);
    }

    /// <summary>
    /// Creates a new <see cref="BitMask"/> containing only unset bits.
    /// </summary>
    /// <param name="bitCount">The number of bits in the <see cref="BitMask"/>.</param>
    /// <returns>A new <see cref="BitMask"/>.</returns>
    public static BitMask Zero(int bitCount)
    {
        var bucketCount = BitOps.GetBucketCount(bitCount);
        var mask = new ulong[bucketCount];
        var immutable = ImmutableCollectionsMarshal.AsImmutableArray(mask);
        return new BitMask(immutable);
    }
    #endregion

    #region Equatable
    public bool Equals(BitMask other)
    {
        if (Buckets.Equals(other.Buckets))
        {
            return true;
        }

        if (Buckets.IsDefault || other.Buckets.IsDefault)
        {
            return false;
        }

        return Buckets.AsSpan().SequenceEqual(other);
    }

    public override int GetHashCode()
    {
        if (Buckets.IsDefaultOrEmpty)
        {
            return 0;
        }

        var hash = new HashCode();
        for (int i = 0; i < Buckets.Length; i++)
        {
            hash.Add(Buckets[i]);
        }

        return hash.ToHashCode();
    }
    #endregion

    #region Types
    /// <summary>
    /// A value representing a 64 bit bucket in a bit mask.
    /// </summary>
    /// <param name="BucketIndex">The index of the bucket.</param>
    /// <param name="BucketValue">The value of the bucket.</param>
    public readonly record struct BucketMask(int BucketIndex, ulong BucketValue)
    {
        /// <summary>
        /// Creates a new <see cref="BucketMask"/> representing a bucket with a single bit set.
        /// </summary>
        /// <param name="bitIndex">The index of the set bit in relation to a full <see cref="BitMask"/>.</param>
        /// <returns>A <see cref="BucketMask"/> representing the specific set bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BucketMask FromBitIndex(int bitIndex)
        {
            return new(
                BitOps.GetBucketIndex(bitIndex),
                BitOps.GetBucketValue(bitIndex));
        }
    }
    #endregion

    #region Enumerators
    /// <summary>
    /// An enumerator iterating through all set bits of a <see cref="BitMask"/>.
    /// </summary>
    /// <param name="buckets">The <see cref="BitMask"/> to iterate through.</param>
    public ref struct BitMaskEnumerator(ReadOnlySpan<ulong> buckets)
    {
        private readonly ReadOnlySpan<ulong> buckets = buckets;
        private int bucketIndex = 0;
        private ulong currentBucket = 0;
        private int currentBit = -1;

        /// <summary>
        /// The index of the current set bit.
        /// </summary>
        public readonly int Current => ((bucketIndex - 1) << 6) | currentBit;

        /// <summary>
        /// Move to the index of the next set bit.
        /// </summary>
        /// <returns>True if there are any remaining set bits in the <see cref="BitMask"/>; otherwise False.</returns>
        public bool MoveNext()
        {
            while (currentBucket == 0UL)
            {
                if (bucketIndex >= buckets.Length)
                {
                    return false;
                }

                currentBucket = buckets[bucketIndex++];
            }

            currentBit = BitOperations.TrailingZeroCount(currentBucket);
            currentBucket &= ~(1UL << currentBit);
            return true;
        }
    }
    #endregion

    #region Builders
    /// <summary>
    /// A builder used to create a new <see cref="BitMask"/>.
    /// </summary>
    public class Builder
    {
        private readonly int initializedBucketCount;
        private ulong[]? buckets;

        /// <summary>
        /// Indicates if all bits of the <see cref="Builder"/> is set to zero.
        /// </summary>
        public bool IsZero => BitOps.IsZero(buckets);

        /// <summary>
        /// Indicates if the <see cref="Builder"/> already has been used to produce a <see cref="BitMask"/>.
        /// </summary>
        [MemberNotNullWhen(false, nameof(buckets))]
        public bool IsConsumed => buckets is null;

        /// <summary>
        /// Creates a new <see cref="Builder"/> with an unrestricted bucket count.
        /// </summary>
        /// <remarks>
        /// This requires calling <see cref="ToBitMask(int)"/> and specifying the bucket count when producing the <see cref="BitMask"/>.
        /// </remarks>
        public Builder()
        {
            initializedBucketCount = -1;
            buckets = [];
        }

        /// <summary>
        /// Creates a new <see cref="Builder"/> with a restricted bucket count.
        /// </summary>
        /// <remarks>
        /// This allows calling <see cref="ToBitMask()"/> when producing the <see cref="BitMask"/>.
        /// </remarks>
        public Builder(int bucketCount)
        {
            initializedBucketCount = bucketCount;
            buckets = new ulong[bucketCount];
        }

        /// <summary>
        /// Sets a specified bit in the <see cref="Builder"/> to 1.
        /// </summary>
        /// <param name="index">The index of the bit to set.</param>
        /// <returns>True if the bit was NOT already set; otherwise False.</returns>
        /// <exception cref="InvalidOperationException">If the <see cref="Builder"/> is consumed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the index is not a positive integer.</exception>
        public bool Set(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be a positive integer or zero.");
            }

            var requiredBucketCount = BitOps.GetBucketCount(index + 1);
            ReziseIfRequired(requiredBucketCount);
            return BitOps.Set(buckets, index);
        }

        /// <summary>
        /// Performs the AND NOT operation on the <see cref="Builder"/> with the value of <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The <see cref="BitMask"/> to use when performing the AND NOT operation on the <see cref="Builder"/>.</param>
        /// <exception cref="InvalidOperationException">If the <see cref="Builder"/> is consumed.</exception>
        public void AndNot(BitMask other)
        {
            ReziseIfRequired(other.BucketCount);
            for (int i = 0; i < other.BucketCount; i++)
            {
                BitOps.AndNotBucketAt(buckets, i, other);
            }
        }

        /// <summary>
        /// Produces a <see cref="BitMask"/> from the <see cref="Builder"/>.
        /// </summary>
        /// <remarks>
        /// This will consume the <see cref="Builder"/> making it unusable.
        /// </remarks>
        /// <returns>A new <see cref="BitMask"/>.</returns>
        /// <exception cref="InvalidOperationException">If the <see cref="Builder"/> is consumed, the bucket count is unknown or the
        /// underlying array is longer than the number of buckets the <see cref="Builder"/> was initialized to support.</exception>
        public BitMask ToBitMask()
        {
            ThrowIfConsumed();

            if (initializedBucketCount == -1)
            {
                throw new InvalidOperationException("Can not create a bit mask with an unknown bucket count.");
            }

            if (buckets.Length > initializedBucketCount)
            {
                throw new InvalidOperationException($"The mask grew to {buckets.Length} buckets, which exceeds the expected {initializedBucketCount}.");
            }

            return ToBitMask(initializedBucketCount);
        }

        /// <summary>
        /// Produces a <see cref="BitMask"/> from the <see cref="Builder"/> with exactly <paramref name="exactBucketCount"/> of
        /// buckets.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>Buckets added for padding will be zero.</item>
        /// <item>This will consume the <see cref="Builder"/> making it unusable.</item>
        /// </list>
        /// </remarks>
        /// <param name="exactBucketCount">The exact number of buckets of the resulting <see cref="BitMask"/>.</param>
        /// <returns>A new <see cref="BitMask"/>.</returns>
        /// <exception cref="InvalidOperationException">If the <see cref="Builder"/> is consumed.</exception>
        public BitMask ToBitMask(int exactBucketCount)
        {
            ReziseIfRequired(exactBucketCount);
            var immutable = ImmutableCollectionsMarshal.AsImmutableArray(buckets);
            buckets = null;
            return new BitMask(immutable);
        }

        /// <summary>
        /// Creates a new <see cref="Builder"/> initialized with the bits set in <paramref name="mask"/>.
        /// </summary>
        /// <param name="mask">The <see cref="BitMask"/> to initialize the builder with.</param>
        /// <returns>A new instance of <see cref="Builder"/> with the bits set in <paramref name="mask"/> set.</returns>
        public static Builder From(BitMask mask)
        {
            var builder = new Builder(mask.BucketCount);
#pragma warning disable CS8604 // builder.buckets can not be null on a fresh instance.
            mask.Buckets.CopyTo(builder.buckets);
#pragma warning restore CS8604
            return builder;
        }

        /// <summary>
        /// Resizes the underlying array if the required length is greater than the current length of the underlying array.
        /// </summary>
        /// <param name="requiredBucketCount">The required length of the underlying array.</param>
        /// <exception cref="InvalidOperationException">If the builder is consumed.</exception>
        [MemberNotNull(nameof(buckets))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReziseIfRequired(int requiredBucketCount)
        {
            ThrowIfConsumed();

            if (buckets.Length < requiredBucketCount)
            {
                Array.Resize(ref buckets, requiredBucketCount);
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the builder has been consumed.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the builder is consumed.</exception>
        [MemberNotNull(nameof(buckets))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfConsumed()
        {
            if (IsConsumed)
            {
                throw new InvalidOperationException("Can not operate on a builder that is consumed.");
            }
        }
    }
    #endregion

    #region Workspace
    /// <summary>
    /// A mutable workspace to perform allocation free optimized bitwise operations on a <see cref="BitMask"/>.
    /// </summary>
    public readonly ref struct Workspace
    {
        private readonly ulong[] buckets;

        /// <summary>
        /// Creates a <see cref="Workspace"/> of a specified length with all bits set to zero.
        /// </summary>
        /// <param name="bucketCount">The number of buckets in the <see cref="Workspace"/>.</param>
        public Workspace(int bucketCount)
        {
            buckets = new ulong[bucketCount];
        }

        /// <summary>
        /// Creates a <see cref="Workspace"/> from a <see cref="BitMask"/>.
        /// </summary>
        /// <param name="mask">The <see cref="BitMask"/> to initialize the workspace with.</param>
        public Workspace(BitMask mask)
        {
            buckets = new ulong[mask.BucketCount];
            mask.Buckets.CopyTo(buckets);
        }

        /// <summary>
        /// Performs the AND operation on the bucket at <paramref name="bucketIndex"/> in the
        /// <see cref="Workspace"/> with the bucket at the same index in <paramref name="other"/>.
        /// </summary>
        /// <param name="bucketIndex">The index of the buckets.</param>
        /// <param name="other">The <see cref="BitMask"/> containing to use.</param>
        /// <exception cref="InvalidOperationException">If the length of the underlying arrays of this <see cref="Workspace"/> and <paramref name="other"/> differs.</exception>
        /// <exception cref="IndexOutOfRangeException">If <paramref name="bucketIndex"/> is outside the bounds of the underlying array.</exception>
        public void AndBucketAt(int bucketIndex, BitMask other)
        {
            BitOps.ThrowIfDifferentLengthOrOutOfBounds(buckets, other, bucketIndex, nameof(AndBucketAt));
            BitOps.AndBucketAt(buckets, bucketIndex, other);
        }

        /// <summary>
        /// Performs the AND NOT operation on the bucket at <paramref name="bucketIndex"/> in the
        /// <see cref="Workspace"/> with the bucket at the same index in <paramref name="other"/>.
        /// </summary>
        /// <param name="bucketIndex">The index of the buckets.</param>
        /// <param name="other">The <see cref="BitMask"/> containing to use.</param>
        /// <exception cref="InvalidOperationException">If the length of the underlying arrays of this <see cref="Workspace"/> and <paramref name="other"/> differs.</exception>
        /// <exception cref="IndexOutOfRangeException">If <paramref name="bucketIndex"/> is outside the bounds of the underlying array.</exception>
        public void AndNotBucketAt(int bucketIndex, BitMask other)
        {
            BitOps.ThrowIfDifferentLengthOrOutOfBounds(buckets, other, bucketIndex, nameof(AndNotBucketAt));
            BitOps.AndNotBucketAt(buckets, bucketIndex, other);
        }

        /// <summary>
        /// Performs the OR operation on the bucket at <paramref name="bucketIndex"/> in the
        /// <see cref="Workspace"/> with the bucket at the same index in <paramref name="other"/>.
        /// </summary>
        /// <param name="bucketIndex">The index of the buckets.</param>
        /// <param name="other">The <see cref="BitMask"/> containing to use.</param>
        /// <exception cref="InvalidOperationException">If the length of the underlying arrays of this <see cref="Workspace"/> and <paramref name="other"/> differs.</exception>
        /// <exception cref="IndexOutOfRangeException">If <paramref name="bucketIndex"/> is outside the bounds of the underlying array.</exception>
        public void OrBucketAt(int bucketIndex, BitMask other)
        {
            BitOps.ThrowIfDifferentLengthOrOutOfBounds(buckets, other, bucketIndex, nameof(OrBucketAt));
            BitOps.OrBucketAt(buckets, bucketIndex, other);
        }

        /// <summary>
        /// Clears the workspace setting all bits to zero.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = 0UL;
            }
        }

        /// <summary>
        /// Creates an enumerator looping through the indicies of all set bits in the <see cref="Workspace"/>.
        /// </summary>
        /// <returns>A new <see cref="BitMaskEnumerator"/>.</returns>
        public BitMaskEnumerator GetEnumerator() => new(buckets);
    }
    #endregion
}

/// <summary>
/// Contains the consolidated static methods for the bitwise operations for use in the <see cref="BitMask"/> struct and its helpers.
/// </summary>
file static class BitOps
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBucketCount(int bitCount) => ((bitCount - 1) >> 6) + 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetBucketIndex(int bitIndex)
    {
        return bitIndex >> 6;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetBucketValue(int bitIndex)
    {
        return 1UL << (bitIndex & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(ReadOnlySpan<ulong> mask, int bitIndex)
    {
        return InternalIsSet(mask, BitMask.BucketMask.FromBitIndex(bitIndex));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsZero(ReadOnlySpan<ulong> mask)
    {
        foreach (var bucket in mask)
        {
            if (bucket != 0UL)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Set(Span<ulong> target, int bitIndex)
    {
        var bucketMask = BitMask.BucketMask.FromBitIndex(bitIndex);
        if (InternalIsSet(target, bucketMask))
        {
            return false;
        }

        target[bucketMask.BucketIndex] |= bucketMask.BucketValue;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AndBucketAt(Span<ulong> target, int bucketIndex, ReadOnlySpan<ulong> other)
    {
        target[bucketIndex] &= other[bucketIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AndNotBucketAt(Span<ulong> target, int bucketIndex, ReadOnlySpan<ulong> other)
    {
        target[bucketIndex] &= ~other[bucketIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OrBucketAt(Span<ulong> target, int bucketIndex, ReadOnlySpan<ulong> other)
    {
        target[bucketIndex] |= other[bucketIndex];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSubsetOf(ReadOnlySpan<ulong> self, ReadOnlySpan<ulong> other)
    {
        for (int i = 0; i < self.Length; i++)
        {
            if ((self[i] & ~other[i]) != 0)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDifferentLengthOrOutOfBounds(ReadOnlySpan<ulong> a, ReadOnlySpan<ulong> b, int bucketIndex, string operationName)
    {
        ThrowIfDifferentLength(a, b, operationName);
        ThrowIfOutOfBounds(bucketIndex, a);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDifferentLength(ReadOnlySpan<ulong> a, ReadOnlySpan<ulong> b, string operationName)
    {
        if (a.Length != b.Length)
        {
            throw new InvalidOperationException($"Can not perform '{operationName}' on masks with different lengths.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfOutOfBounds(int bucketIndex, ReadOnlySpan<ulong> mask)
    {
        if (bucketIndex < 0 || bucketIndex >= mask.Length)
        {
            throw new IndexOutOfRangeException("The index was outside the bounds of the array.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InternalIsSet(ReadOnlySpan<ulong> target, BitMask.BucketMask bucketMask)
    {
        return (target[bucketMask.BucketIndex] & bucketMask.BucketValue) != 0;
    }
}
