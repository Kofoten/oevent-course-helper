using OEventCourseHelper.Data;
using System.Collections.Immutable;
using System.Numerics;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal record CourseMask(string CourseName, ImmutableArray<ulong> ControlMask, int ControlCount)
{
    public void ForEachControl<T>(ref T processor) where T : struct, IProcessor
    {
        for (int i = 0; i < ControlMask.Length; i++)
        {
            ulong bucket = ControlMask[i];
            while (bucket != 0)
            {
                int bit = BitOperations.TrailingZeroCount(bucket);
                int index = (i << 6) | bit;
                processor.Process(index);
                bucket &= ~(1UL << bit);
            }
        }
    }

    public bool IsSubsetOf(CourseMask other)
    {
        for (int i = 0; i < ControlMask.Length; i++)
        {
            if ((ControlMask[i] & ~other.ControlMask[i]) != 0)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsIdenticalTo(CourseMask other)
    {
        for (int i = 0; i < ControlMask.Length; i++)
        {
            if (ControlMask[i] != other.ControlMask[i])
            {
                return false;
            }
        }

        return true;
    }
}
