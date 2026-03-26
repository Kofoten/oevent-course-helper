using Microsoft.Extensions.DependencyInjection;
using OEventCourseHelper.Logging.Porcelain;

namespace OEventCourseHelper.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPorcelainFormatters(this IServiceCollection services, Action<PorcelainFormatterRegistry> configure)
    {
        return services.AddSingleton((_) =>
        {
            var registry = new PorcelainFormatterRegistry();
            configure.Invoke(registry);
            return registry;
        });
    }
}
