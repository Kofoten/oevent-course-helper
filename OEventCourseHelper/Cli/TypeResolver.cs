using Spectre.Console.Cli;

namespace OEventCourseHelper.Cli;

internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver
{
    public object? Resolve(Type? type) => type == null ? null : provider.GetService(type);
}
