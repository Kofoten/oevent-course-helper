using System.Collections.Immutable;
using System.Numerics;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal readonly record struct BitMask
{
    public BitMask(ImmutableArray<ulong> buckets)
    {
        Buckets = buckets;
    }

    public readonly ImmutableArray<ulong> Buckets { get; private init; }

    public int Length { get; private init; }

    public bool this[int index] => IsSet(Buckets.AsSpan(), index);

    public bool IsZero => Buckets.All(x => x == 0);

    public int BucketCount => Buckets.Length;

    public BitMaskEnumerator GetEnumerator() => new(Buckets);

    public BitMask And(BitMask other)
    {
        ThrowIfDifferentLength(other, nameof(And));

        var result = new ulong[BucketCount];
        for (int i = 0; i < BucketCount; i++)
        {
            result[i] = Buckets[i] & other.Buckets[i];
        }

        return Create(result);
    }

    public BitMask AndNot(BitMask other)
    {
        ThrowIfDifferentLength(other, nameof(AndNot));

        var result = new ulong[BucketCount];
        for (int i = 0; i < BucketCount; i++)
        {
            result[i] = Buckets[i] & other.Buckets[i];
        }

        return Create(result);
    }

    public BitMask Set(int index)
    {
        var mask = new ulong[BucketCount];
        Buckets.CopyTo(mask, 0);

        Set(mask, index);

        return Create(mask);
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

    /// <summary>
    /// Checks if <paramref name="other"/> is identical to this.
    /// </summary>
    /// <param name="other">The <see cref="BitMask"/> to compare to.</param>
    /// <returns>True if <paramref name="other"/> is identical to this; otherwise False.</returns>
    public bool IsIdenticalTo(BitMask other)
    {
        ThrowIfDifferentLength(other, nameof(IsIdenticalTo));

        for (int i = 0; i < BucketCount; i++)
        {
            if (Buckets[i] != other.Buckets[i])
            {
                return false;
            }
        }

        return true;
    }

    public static void Set(Span<ulong> mask, int index)
    {
        ThrowIfOutsideBounds(mask, index);

        mask[index >> 6] |= 1UL << (index & 63);
    }

    public static bool IsSet(ReadOnlySpan<ulong> mask, int index)
    {
        ThrowIfOutsideBounds(mask, index);

        return (mask[index >> 6] & (1UL << (index & 63))) != 0;
    }

    private void ThrowIfDifferentLength(BitMask other, string operationName)
    {
        if (BucketCount != other.BucketCount)
        {
            throw new InvalidOperationException($"Can not perform '{operationName}' on BitMasks of different lengths.");
        }
    }

    private static void ThrowIfOutsideBounds(ReadOnlySpan<ulong> mask, int index)
    {
        if (index < 0 || index >= (mask.Length << 6))
        {
            throw new IndexOutOfRangeException("The index was outside the bounds of the array.");
        }
    }

    public static int GetBucketCount(int bitCount) => ((bitCount - 1) >> 6) + 1;

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

        return Create(mask);
    }

    public static BitMask Create(ReadOnlySpan<ulong> buckets)
        => new(ImmutableArray.Create(buckets));

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

    public ref struct BucketEnumerator(int bucketIndex, ulong bucket)
    {
        private readonly int bucketMask = bucketIndex << 6;
        private int currentBit = -1;

        public readonly int Current => bucketMask | currentBit;

        public bool MoveNext()
        {
            if (bucket == 0UL)
            {
                return false;
            }

            currentBit = BitOperations.TrailingZeroCount(bucket);
            bucket &= ~(1UL << currentBit);
            return true;
        }
    }
}
