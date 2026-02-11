using System.Collections.Frozen;

namespace OEventCourseHelper.Data;

internal record Course(string Name, FrozenSet<string> Controls)
{
    public static Course FromIOF(IOF.Xml.Course iofCourse)
    {
        var controls = iofCourse.CourseControl
            .Where(x => x.type == IOF.Xml.ControlType.Control)
            .SelectMany(x => x.Control)
            .ToFrozenSet();

        return new Course(iofCourse.Name, controls);
    }
}
