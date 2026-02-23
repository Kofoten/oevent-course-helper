namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal readonly struct CandidateBlueprint
{
    private readonly CandidateSolution parent;
    private readonly Course? addedCourse;

    public float RarityScore { get; private init; }
    public int CourseCount { get; private init; }

    public CandidateBlueprint(CandidateSolution parent)
    {
        this.parent = parent;
        addedCourse = null;
        RarityScore = parent.RarityScore;
        CourseCount = parent.CourseCount;
    }

    public CandidateBlueprint(CandidateSolution parent, Course addedCourse, float projectedRarityScore)
    {
        this.parent = parent;
        this.addedCourse = addedCourse;
        RarityScore = projectedRarityScore;
        CourseCount = parent.CourseCount + 1;
    }

    public CandidateSolution Materialize(BeamSearchSolverContext context)
    {
        if (addedCourse is null)
        {
            return parent;
        }

        var unvisitedControlsMaskBuilder = BitMask.Builder.From(parent.UnvisitedControlsMask);
        var rarityGain = 0.0F;

        for (int i = 0; i < context.ControlMaskBucketCount; i++)
        {
            var overlap = parent.UnvisitedControlsMask.Buckets[i] & addedCourse.ControlMask.Buckets[i];
            var bucketEnumerator = new BitMask.BucketEnumerator(i, overlap);
            while (bucketEnumerator.MoveNext())
            {
                rarityGain += context.ControlRarityLookup[bucketEnumerator.Current];
            }

            unvisitedControlsMaskBuilder.AndNotBucketAt(i, addedCourse.ControlMask);
        }

        var includedCoursesMaskBuilder = BitMask.Builder.From(parent.IncludedCoursesMask);
        includedCoursesMaskBuilder.Set(addedCourse.CourseIndex);

        return new CandidateSolution(
            parent.CourseOrder.Add(addedCourse),
            includedCoursesMaskBuilder.ToBitMask(),
            unvisitedControlsMaskBuilder.ToBitMask(),
            RarityScore - rarityGain);
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

            return x.CourseCount.CompareTo(y.CourseCount);
        }
    }
}
