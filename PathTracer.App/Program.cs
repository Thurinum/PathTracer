using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PathTracerApp;
using PathTracerApp.Shader;

ServiceCollection services = new();

services.AddLogging(builder => builder.AddConsole());
services.AddSingleton<SlangCompiler>();
services.Configure<AppOptions>(options =>
{
    options.WindowWidth = 1920;
    options.WindowHeight = 1080;
    options.ThreadGroupSize = (8, 8, 1);
});
services.AddSingleton<App>();
services.AddSingleton<PathTracer>();

using ServiceProvider provider = services.BuildServiceProvider();

App app = provider.GetRequiredService<App>();
PathTracer pathTracer = provider.GetRequiredService<PathTracer>();

app.Run(pathTracer.Render);
