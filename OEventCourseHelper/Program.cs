using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OEventCourseHelper.Cli;
using OEventCourseHelper.Commands.CoursePrioritizer;
using OEventCourseHelper.Data;
using Spectre.Console.Cli;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();

var services = new ServiceCollection();
services.AddSingleton(loggerFactory);
services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.AddCommand<CoursePrioritizerCommand>("prioritize");
    config.SetExceptionHandler((ex, _) =>
    {
        logger.LogCritical(ex, "An unexpected error occurred.");
        return ExitCode.UnhandledException;
    });
});

return app.Run(args);
