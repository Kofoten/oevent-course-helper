using System.Collections.Immutable;
using System.Numerics;

namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// Contains the bitmask for the course, the name of the course and the number of controls in the course.
/// </summary>
internal record CourseMask(string CourseName, ImmutableArray<ulong> ControlMask, int ControlCount)
{
    /// <summary>
    /// Loops through all the controls in this <see cref="CourseMask"> one by one for processing by a provided processor.
    /// </summary>
    /// <typeparam name="T">The type of processor to use.</typeparam>
    /// <param name="processor">The processor to use.</param>
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

    /// <summary>
    /// Checks if <paramref name="other"/> is a subset of this.
    /// </summary>
    /// <param name="other">The <see cref="CourseMask"/> to check.</param>
    /// <returns>True if <paramref name="other"/> is a subset of this; otherwise False.</returns>
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

    /// <summary>
    /// Checks if <paramref name="other"/> is identical to this.
    /// </summary>
    /// <param name="other">The <see cref="CourseMask"/> to compare to.</param>
    /// <returns>True if <paramref name="other"/> is identical to this; otherwise False.</returns>
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


    /// <summary>
    /// Builder for <see cref="CourseMask"/>.
    /// </summary>
    internal class Builder()
    {
        public string CourseName { get; set; } = "Unknown Course";
        public IList<ulong> ControlMask { get; set; } = [];
        public int ControlCount { get; set; } = 0;

        /// <summary>
        /// Builds the <see cref="CourseMask"/> record.
        /// </summary>
        /// <param name="bucketCount">Total count of 64 bit buckets.</param>
        /// <returns>An instance of <see cref="CourseMask"/>.</returns>
        public CourseMask ToCourseMask(int bucketCount)
        {
            var maskBuilder = ImmutableArray.CreateBuilder<ulong>(bucketCount);
            for (int i = 0; i < bucketCount; i++)
            {
                if (i < ControlMask.Count)
                {
                    maskBuilder.Add(ControlMask[i]);
                }
                else
                {
                    maskBuilder.Add(0UL);
                }
            }

            return new CourseMask(
                CourseName,
                maskBuilder.DrainToImmutable(),
                ControlCount);
        }
    }

    /// <summary>
    /// Interface for use with <see cref="ForEachControl"/>.
    /// </summary>
    internal interface IProcessor
    {
        void Process(int controlIndex);
    }
}
