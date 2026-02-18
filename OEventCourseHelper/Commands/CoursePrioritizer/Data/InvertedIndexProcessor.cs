namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

/// <summary>
/// A processor to be used for populating an inverted index.
/// </summary>
/// <param name="InvertedIndex">The inverted index to populate.</param>
/// <param name="BucketCount">The count of buckets required for the course id mask.</param>
internal struct InvertedIndexProcessor(ulong[][] InvertedIndex, int BucketCount) : CourseMask.IProcessor
{
    public readonly void Process(int index, CourseMask courseMask)
    {
        if (InvertedIndex[index] is null || InvertedIndex.Length == 0)
        {
            InvertedIndex[index] = new ulong[BucketCount];
        }

        InvertedIndex[index][courseMask.CourseId.BucketIndex] |= courseMask.CourseId.BucketMask;
    }
}
