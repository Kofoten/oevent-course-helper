using Spectre.Console.Cli;

namespace OEventCourseHelper.Extensions;

internal static class TypeResolverExtensions
{
    public static T? Resolve<T>(this ITypeResolver? resolver)
        where T : class
    {
        var resolved = resolver?.Resolve(typeof(T));
        if (resolved is null)
        {
            return null;
        }

        return resolved as T;
    }
}
