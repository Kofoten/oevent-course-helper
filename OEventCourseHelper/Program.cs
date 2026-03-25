using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OEventCourseHelper.Cli;
using OEventCourseHelper.Commands.CoursePrioritizer;
using OEventCourseHelper.Logging;
using Spectre.Console.Cli;

var services = new ServiceCollection();

services.Configure<OEventCourseHelperLoggingOptions>(_ => { });

services.AddLogging(builder =>
{
    builder.AddConsoleFormatter<OEventCourseHelperConsoleFormatter, ConsoleFormatterOptions>();
    builder.AddConsole(options =>
    {
        options.FormatterName = OEventCourseHelperConsoleFormatter.FormatterName;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.AddCommand<CoursePrioritizerCommand>("prioritize");
    config.SetExceptionHandler(CliUtilities.ExceptionHandler);
});

return app.Run(args);
