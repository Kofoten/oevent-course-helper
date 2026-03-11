using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal readonly record struct BitMask : IEquatable<BitMask>
{
    public BitMask(ImmutableArray<ulong> buckets)
    {
        Buckets = buckets;
        IsZero = BitOps.IsZero(this);
    }

    public readonly ImmutableArray<ulong> Buckets { get; private init; }

    public bool IsZero { get; private init; }

    public bool this[int bitIndex] => BitOps.IsSet(this, bitIndex);

    public int BucketCount => Buckets.Length;

    public BitMaskEnumerator GetEnumerator() => new(this);

    public static implicit operator ReadOnlySpan<ulong>(BitMask mask) => mask.Buckets.AsSpan();

    public BitMask Set(int bitIndex)
    {
        var bucketIndex = BitOps.GetBucketIndex(bitIndex);
        BitOps.ThrowIfOutOfBounds(bucketIndex, this);

        var builder = Builder.From(this);
        builder.Set(bitIndex);
        return builder.ToBitMask();
    }

    public BitMask AndNot(BitMask other)
    {
        BitOps.ThrowIfDifferentLength(this, other, nameof(AndNot));

        var builder = Builder.From(this);
        builder.AndNot(other);
        return builder.ToBitMask();
    }

    public bool IsSubsetOf(BitMask other)
    {
        BitOps.ThrowIfDifferentLength(this, other, nameof(IsSubsetOf));

        return BitOps.IsSubsetOf(this, other);
    }

    public static int GetBucketCount(int bitCount) => BitOps.GetBucketCount(bitCount);

    #region Factories
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
    public readonly record struct BucketMask(int BucketIndex, ulong BucketValue)
    {
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
    public ref struct BitMaskEnumerator(ReadOnlySpan<ulong> buckets)
    {
        private readonly ReadOnlySpan<ulong> buckets = buckets;
        private int bucketIndex = 0;
        private ulong currentBucket = 0;
        private int currentBit = -1;

        public readonly int Current => ((bucketIndex - 1) << 6) | currentBit;

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
    public class Builder
    {
        private readonly int initializedBucketCount;
        private ulong[]? buckets;

        public bool IsZero => BitOps.IsZero(buckets);

        [MemberNotNullWhen(false, nameof(buckets))]
        public bool IsConsumed => buckets is null;

        public Builder()
        {
            initializedBucketCount = -1;
            buckets = [];
        }

        public Builder(int bucketCount)
        {
            initializedBucketCount = bucketCount;
            buckets = new ulong[bucketCount];
        }

        [MemberNotNull(nameof(buckets))]
        public void Initialize(ulong[] buckets)
        {
            this.buckets = buckets;
        }

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

        public void AndNot(BitMask other)
        {
            ReziseIfRequired(other.BucketCount);
            for (int i = 0; i < other.BucketCount; i++)
            {
                BitOps.AndNotBucketAt(buckets, i, other);
            }
        }

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

        public BitMask ToBitMask(int exactBucketCount)
        {
            ReziseIfRequired(exactBucketCount);
            var immutable = ImmutableCollectionsMarshal.AsImmutableArray(buckets);
            buckets = null;
            return new BitMask(immutable);
        }

        public static Builder From(BitMask mask)
        {
            var builder = new Builder(mask.BucketCount);
#pragma warning disable CS8604 // builder.buckets can not be null on a fresh instance.
            mask.Buckets.CopyTo(builder.buckets);
#pragma warning restore CS8604
            return builder;
        }

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
    public readonly ref struct Workspace(int bucketCount)
    {
        private readonly ulong[] buckets = new ulong[bucketCount];

        public void AndBucketAt(int bucketIndex, BitMask other)
        {
            BitOps.ThrowIfDifferentLengthOrOutOfBounds(buckets, other, bucketIndex, nameof(AndBucketAt));
            BitOps.AndBucketAt(buckets, bucketIndex, other);
        }

        public void AndNotBucketAt(int bucketIndex, BitMask other)
        {
            BitOps.ThrowIfDifferentLengthOrOutOfBounds(buckets, other, bucketIndex, nameof(AndNotBucketAt));
            BitOps.AndNotBucketAt(buckets, bucketIndex, other);
        }

        public void OrBucketAt(int bucketIndex, BitMask other)
        {
            BitOps.ThrowIfDifferentLengthOrOutOfBounds(buckets, other, bucketIndex, nameof(OrBucketAt));
            BitOps.OrBucketAt(buckets, bucketIndex, other);
        }

        public void Clear()
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = 0UL;
            }
        }

        public BitMaskEnumerator GetEnumerator() => new(buckets);
    }
    #endregion
}

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
