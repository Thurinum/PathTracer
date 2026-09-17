using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PathTracerApp;
using PathTracerApp.Renderer;
using PathTracerApp.Renderer.Pass;
using PathTracerApp.SceneGraph;
using PathTracerApp.Shader;
using PathTracerSceneGraph;

using var provider = ConfigureServices();
var app = provider.GetRequiredService<App>();
app.Run();
return;

ServiceProvider ConfigureServices()
{
    ServiceCollection services = new();

    services.AddLogging(builder => builder.AddConsole());
    services.AddSingleton<SlangCompiler>();
    services.Configure<RenderBackendOptions>(options =>
    {
        options.WindowWidth = 1920;
        options.WindowHeight = 1080;
        options.X = 100;
        options.Y = 100;
        options.ThreadGroupSize = (8, 8, 1);
    });
    services.AddSingleton<RenderBackend>();
    services.AddSingleton<RenderState>();
    services.AddSingleton(typeof(RenderPrimitives<>), typeof(RenderPrimitives<>));    
    services.AddSingleton<RenderPassFactory>();
    services.AddSingleton<ComponentFactory>();
    services.AddSingleton<SceneManager>();
    services.AddSingleton<SceneLoop>();

    services.AddSingleton<App>();

    return services.BuildServiceProvider();
}
