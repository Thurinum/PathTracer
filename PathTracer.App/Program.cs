using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PathTracerApp;
using PathTracerApp.RenderPasses;
using PathTracerCore;

using var provider = ConfigureServices();
var app = provider.GetRequiredService<App>();
app.Run();
return;

ServiceProvider ConfigureServices()
{
    ServiceCollection services = new();

    services.AddLogging(builder => builder.AddConsole());
    services.AddEngineCore(options =>
    {
        options.WindowWidth = 1920;
        options.WindowHeight = 1080;
        options.X = 100;
        options.Y = 100;
    }, passes =>
    {
        passes.AddPass<SpheresRenderPass>();
        passes.AddPass<PlanesRenderPass>();
    });
    services.AddSingleton<App>();
        
    return services.BuildServiceProvider();
}
