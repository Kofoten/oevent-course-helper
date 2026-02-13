namespace OEventCourseHelper.Commands.CoursePrioritizer.Data;

internal record CourseMaskNodeReaderResult
{
    public required IEnumerable<CourseMask> CourseMasks { get; init; }
    public required int TotalEventControlCount { get; init; }
}