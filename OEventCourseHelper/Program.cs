using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OEventCourseHelper.Cli;
using OEventCourseHelper.Commands.CoursePrioritizer;
using Spectre.Console.Cli;

var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
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
