using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal readonly struct BitMask(ImmutableArray<ulong> Buckets)
{
    public BitMask(ulong[] buckets)
        : this(ImmutableCollectionsMarshal.AsImmutableArray(buckets))
    {
    }

    //public BitMask(int setBits)
    //{
    //}

    public bool this[int index] => (Buckets[index >> 6] & (1UL << (index & 63))) != 0;

    public bool IsZero => Buckets.All(x => x == 0);

    public BitMaskEnumerator GetEnumerator() => new(Buckets);

    public static int GetBucketCount(int bitCount) => ((bitCount - 1) >> 6) + 1;

    public ref struct BitMaskEnumerator(ImmutableArray<ulong> buckets)
    {
        private readonly ImmutableArray<ulong> buckets = buckets;
        private int bucketIndex = 0;
        private ulong currentBucket = 0;
        private int currentBit = -1;

        public readonly int Current => ((bucketIndex - 1) << 6) | currentBit;

        public bool MoveNext()
        {
            while (currentBucket == 0)
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
}
