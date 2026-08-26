using Kofoten.NativeCli;
using Kofoten.NativeCli.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using OEventCourseHelper.Cli;
using OEventCourseHelper.Commands.CoursePrioritizer;
using OEventCourseHelper.Logging;
using OEventCourseHelper.Logging.Porcelain;

var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

return new ServiceCollection()
    .Configure<OEventCourseHelperLoggingOptions>(_ => { })
    .AddSingleton(sp => new ApplicationContext(sp))
    .AddSingleton<IPorcelainFormatter, V1PorcelainFormatter>()
    .AddSingleton<PorcelainFormatterRegistry>()
    .AddLogging(builder =>
    {
        builder.Services.AddSingleton<ConsoleFormatter, OEventCourseHelperConsoleFormatter>();
        builder.AddConsole(options =>
        {
            options.FormatterName = OEventCourseHelperConsoleFormatter.FormatterName;
        });
        builder.SetMinimumLevel(LogLevel.Information);
    })
    .AddCliCommands(args, router =>
    {
        router.MapCoursePrioritizerCommand("prioritize");
    }, OEventCourseHelper.Cli.CliUtilities.ExceptionHandler)
    .BuildServiceProvider()
    .GetRequiredService<CliCommand>()
    .Execute();
