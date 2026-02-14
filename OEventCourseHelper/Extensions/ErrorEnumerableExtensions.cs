using System.Text;

namespace OEventCourseHelper.Extensions;

internal static class ErrorEnumerableExtensions
{
    /// <summary>
    /// Formats the <paramref name="errors"> in the enumerable into a string with each error indented on a new line.
    /// </summary>
    /// <param name="errors">The errors to format.</param>
    /// <returns>A string with each error indented on a new line.</returns>
    public static string FormatErrors(this IEnumerable<string> errors)
    {
        var builder = new StringBuilder();
        foreach (var error in errors)
        {
            builder.AppendFormat("{0}  - {1}", Environment.NewLine, error);
        }
        return builder.ToString();
    }
}
