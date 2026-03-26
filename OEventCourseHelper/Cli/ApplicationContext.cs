using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OEventCourseHelper.Logging;
using OEventCourseHelper.Logging.Porcelain;

namespace OEventCourseHelper.Cli;

internal class ApplicationContext(IServiceProvider serviceProvider)
{
    public bool IsPorcelainVersionSupported(string version)
    {
        var registry = serviceProvider.GetService<PorcelainFormatterRegistry>();
        if (registry is not null)
        {
            return registry.IsVersionSupported(version);
        }

        return false;
    }

    public void SetPorcelainLoggingMode(string version)
    {
        var formatterOptions = serviceProvider.GetService<IOptionsMonitor<OEventCourseHelperLoggingOptions>>();
        if (formatterOptions is not null)
        {
            formatterOptions.CurrentValue.LoggingMode = OEventCourseHelperLoggingMode.Porcelain;
            formatterOptions.CurrentValue.PorcelainVersion = version;
        }
    }
}
