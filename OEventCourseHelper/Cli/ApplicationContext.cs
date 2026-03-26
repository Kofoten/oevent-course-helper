using Microsoft.Extensions.DependencyInjection;
using OEventCourseHelper.Logging.Porcelain;

namespace OEventCourseHelper.Cli;

internal class ApplicationContext(IServiceProvider serviceProvider)
{
    public bool ValidatePorcelainFormatterVersion(string version)
    {
        var registry = serviceProvider.GetService<PorcelainFormatterRegistry>();
        if (registry is not null)
        {
            return registry.IsVersionSupported(version);
        }

        return false;
    }
}
