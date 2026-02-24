using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal readonly record struct BitMask : IEquatable<BitMask>
{
    public BitMask(ImmutableArray<ulong> buckets)
    {
        Buckets = buckets;
        IsZero = IsZeroMask(buckets.AsSpan());
    }

    public readonly ImmutableArray<ulong> Buckets { get; private init; }

    public bool IsZero { get; private init; }

    public bool this[int index] => IsSet(Buckets.AsSpan(), index);

    public int BucketCount => Buckets.Length;

    public BitMaskEnumerator GetEnumerator() => new(Buckets);

    #region Instance Methods
    public BitMask Set(int index)
    {
        var builder = Builder.From(this);
        builder.Set(index);
        return builder.ToBitMask();
    }

    public BitMask AndNot(BitMask other)
    {
        ThrowIfDifferentLength(other, nameof(AndNot));

        var builder = Builder.From(this);
        builder.AndNot(other);
        return builder.ToBitMask();
    }

    /// <summary>
    /// Checks if <paramref name="other"/> is a subset of this.
    /// </summary>
    /// <param name="other">The <see cref="BitMask"/> to check.</param>
    /// <returns>True if <paramref name="other"/> is a subset of this; otherwise False.</returns>
    public bool IsSubsetOf(BitMask other)
    {
        ThrowIfDifferentLength(other, nameof(IsSubsetOf));

        for (int i = 0; i < BucketCount; i++)
        {
            if ((Buckets[i] & ~other.Buckets[i]) != 0)
            {
                return false;
            }
        }

        return true;
    }
    #endregion

    #region Helpers
    public static bool Set(Span<ulong> mask, int index)
    {
        ThrowIfOutOfBounds(mask, index);

        var (bucketIndex, bucketMask) = GetBucketMask(index);
        if (InternalIsSet(mask, bucketIndex, bucketMask))
        {
            return false;
        }

        mask[bucketIndex] |= bucketMask;
        return true;
    }

    public static bool IsSet(ReadOnlySpan<ulong> mask, int index)
    {
        ThrowIfOutOfBounds(mask, index);

        var (bucketIndex, bucketMask) = GetBucketMask(index);
        return InternalIsSet(mask, bucketIndex, bucketMask);
    }

    public static bool IsZeroMask(ReadOnlySpan<ulong> mask)
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

    public static int GetBucketCount(int bitCount) => ((bitCount - 1) >> 6) + 1;
    #endregion

    #region Internals
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int bucketIndex, ulong bucketMask) GetBucketMask(int index) => (index >> 6, 1UL << (index & 63));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InternalIsSet(ReadOnlySpan<ulong> mask, int bucketIndex, ulong bucketMask)
        => (mask[bucketIndex] & bucketMask) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDifferentLength(BitMask other, string operationName)
    {
        if (BucketCount != other.BucketCount)
        {
            throw new InvalidOperationException($"Can not perform '{operationName}' on BitMasks of different lengths.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfOutOfBounds(ReadOnlySpan<ulong> mask, int index)
        => ThrowIfOutOfBounds(mask.Length, index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfOutOfBounds(int length, int index)
    {
        if (index < 0 || index >= (length << 6))
        {
            throw new IndexOutOfRangeException("The index was outside the bounds of the array.");
        }
    }
    #endregion

    #region Factories
    public static BitMask Fill(int count)
    {
        var bucketCount = GetBucketCount(count);
        var mask = new ulong[bucketCount];

        for (int i = 0; i < bucketCount - 1; i++)
        {
            mask[i] = ulong.MaxValue;
        }

        var remainder = count & 63;
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

    public static BitMask Zero(int count)
    {
        var bucketCount = GetBucketCount(count);
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

        if (!Buckets.Length.Equals(other.Buckets.Length))
        {
            return false;
        }

        for (int i = 0; i < Buckets.Length; i++)
        {
            if (!Buckets[i].Equals(other.Buckets[i]))
            {
                return false;
            }
        }

        return true;
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

    #region Enumerators
    public ref struct BitMaskEnumerator(ImmutableArray<ulong> buckets)
    {
        private readonly ImmutableArray<ulong> buckets = buckets;
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
        private ulong[] buckets;

        public bool IsZero => IsZeroMask(buckets);

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

        public bool Set(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index must be a positive integer or zero.");
            }

            var requiredBucketCount = GetBucketCount(index + 1);
            ReziseIfRequired(requiredBucketCount);
            return BitMask.Set(buckets, index);
        }

        public void AndNot(BitMask other)
        {
            ReziseIfRequired(other.BucketCount);
            for (int i = 0; i < other.BucketCount; i++)
            {
                InnerAndNotBucketAt(i, other);
            }
        }

        public void AndNotBucketAt(int bucketIndex, BitMask other)
        {
            if (bucketIndex < 0 || bucketIndex >= other.Buckets.Length)
            {
                throw new IndexOutOfRangeException("The index was outside the bounds of the array.");
            }

            ReziseIfRequired(bucketIndex + 1);
            InnerAndNotBucketAt(bucketIndex, other);
        }

        public void OrBucketAt(int bucketIndex, BitMask other)
        {
            ReziseIfRequired(bucketIndex + 1);
            buckets[bucketIndex] |= other.Buckets[bucketIndex];
        }

        public BitMask ToBitMask()
        {
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
            buckets = [];
            return new BitMask(immutable);
        }

        public static Builder From(BitMask mask)
        {
            var builder = new Builder(mask.BucketCount);
            mask.Buckets.CopyTo(builder.buckets);
            return builder;
        }

        #region Internal
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReziseIfRequired(int requiredBucketCount)
        {
            if (buckets.Length < requiredBucketCount)
            {
                Array.Resize(ref buckets, requiredBucketCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InnerAndNotBucketAt(int bucketIndex, BitMask other)
        {
            buckets[bucketIndex] &= ~other.Buckets[bucketIndex];
        }
        #endregion
    }
    #endregion
}
