namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal readonly struct CandidateBlueprint
{
    private readonly CandidateSolution parent;
    private readonly Course? addedCourse;

    public int CourseCount { get; private init; }
    public ulong RarityScore { get; private init; }

    public CandidateBlueprint(CandidateSolution parent)
    {
        this.parent = parent;
        addedCourse = null;
        CourseCount = parent.CourseCount;
        RarityScore = parent.RarityScore;
    }

    public CandidateBlueprint(CandidateSolution parent, Course addedCourse, ulong projectedRarityScore)
    {
        this.parent = parent;
        this.addedCourse = addedCourse;
        CourseCount = parent.CourseCount + 1;
        RarityScore = projectedRarityScore;
    }

    public CandidateSolution Materialize()
    {
        if (addedCourse is null)
        {
            return parent;
        }

        return new CandidateSolution(
            parent.CourseOrder.Add(addedCourse),
            parent.IncludedCoursesMask.Set(addedCourse.CourseIndex),
            parent.UnvisitedControlsMask.AndNot(addedCourse.ControlMask),
            RarityScore);
    }

    public class RarityComparer : IComparer<CandidateBlueprint>
    {
        public int Compare(CandidateBlueprint x, CandidateBlueprint y)
        {
            var rarityResult = x.RarityScore.CompareTo(y.RarityScore);
            if (rarityResult != 0)
            {
                return rarityResult;
            }

            if (x.addedCourse is null)
            {
                return y.addedCourse is null ? 0 : -1;
            }
            else if (y.addedCourse is null)
            {
                return 1;
            }

            var xBucketMask = BitMask.BucketMask.FromBitIndex(x.addedCourse.CourseIndex);
            var yBucketMask = BitMask.BucketMask.FromBitIndex(y.addedCourse.CourseIndex);

            for (int i = 0; i < x.parent.IncludedCoursesMask.BucketCount; i++)
            {
                ulong xBucket = x.parent.IncludedCoursesMask.Buckets[i];
                if (i == xBucketMask.BucketIndex)
                {
                    xBucket |= xBucketMask.BucketValue;
                }

                ulong yBucket = y.parent.IncludedCoursesMask.Buckets[i];
                if (i == yBucketMask.BucketIndex)
                {
                    yBucket |= yBucketMask.BucketValue;
                }

                var bucketResult = xBucket.CompareTo(yBucket);
                if (bucketResult != 0)
                {
                    return bucketResult;
                }
            }

            return 0;
        }
    }

    public class TieBreakComparer : IComparer<CandidateBlueprint>
    {
        public int Compare(CandidateBlueprint x, CandidateBlueprint y)
        {
            var rarityResult = x.parent.RarityScore.CompareTo(y.parent.RarityScore);
            if (rarityResult != 0)
            {
                return rarityResult;
            }

            if (x.parent.CourseOrder.IsEmpty)
            {
                return y.parent.CourseOrder.IsEmpty ? 0 : -1;
            }
            else if (y.parent.CourseOrder.IsEmpty)
            {
                return 1;
            }

            return x.parent.CourseOrder[0].CourseIndex.CompareTo(y.parent.CourseOrder[0].CourseIndex);
        }
    }
}
