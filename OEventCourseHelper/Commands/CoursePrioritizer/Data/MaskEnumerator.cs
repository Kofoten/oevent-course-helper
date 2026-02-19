using System.Collections;
using System.Collections.Immutable;
using System.Numerics;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal class MaskEnumerator(ImmutableArray<ulong> Mask) : IEnumerator<int>
{
    private int bucketIndex = -1;
    private ulong bucketRemainder = 0UL;

    public int Current { get; private set; } = -1;

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (bucketRemainder == 0UL)
        {
            bucketIndex++;

            if (bucketIndex == Mask.Length)
            {
                return false;
            }

            bucketRemainder = Mask[bucketIndex];
        }

        int bit = BitOperations.TrailingZeroCount(bucketRemainder);
        Current = (bucketIndex << 6) | bit;
        bucketRemainder &= ~(1UL << bit);
        return true;
    }

    public void Reset()
    {
        bucketIndex = -1;
        bucketRemainder = 0UL;
    }

    public void Dispose()
    {
    }
}
