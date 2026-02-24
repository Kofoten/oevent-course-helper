namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal readonly struct CandidateBlueprint
{
    private readonly CandidateSolution parent;
    private readonly Course? addedCourse;


    public int CourseCount { get; private init; }
    public float RarityScore { get; private init; }

    public CandidateBlueprint(CandidateSolution parent)
    {
        this.parent = parent;
        addedCourse = null;
        CourseCount = parent.CourseCount;
        RarityScore = parent.RarityScore;
    }

    public CandidateBlueprint(CandidateSolution parent, Course addedCourse, float projectedRarityScore)
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

            // TODO: Fix exception for empty solution.
            var xCourseIndex = x.parent.CourseOrder[0].CourseIndex;
            var yCourseIndex = y.parent.CourseOrder[0].CourseIndex;
            return xCourseIndex.CompareTo(yCourseIndex);
        }
    }
}
