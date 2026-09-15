using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PathTracerApp;
using PathTracerApp.Renderer;
using PathTracerApp.Shader;
using PathTracerSceneGraph;

ServiceCollection services = new();

services.AddLogging(builder => builder.AddConsole());
services.AddSingleton<SlangCompiler>();
services.Configure<RenderBackendOptions>(options =>
{
    options.WindowWidth = 1920;
    options.WindowHeight = 1080;
    options.ThreadGroupSize = (8, 8, 1);
});
services.AddSingleton<RenderBackend>();
services.AddSingleton<InputSystem>();
services.AddSingleton<RenderPipeline>();
services.AddSingleton<FrameLoop>();
services.AddSingleton<PathTracerResources>();
services.AddSingleton<IFrameRenderer, PathTracer>();
services.AddSingleton<ComponentFactory>();
services.AddSingleton<Scene>();
services.AddSingleton<SceneManager>();
services.AddSingleton<EventLoop>();

using ServiceProvider provider = services.BuildServiceProvider();

FrameLoop frameLoop = provider.GetRequiredService<FrameLoop>();

frameLoop.Run();
