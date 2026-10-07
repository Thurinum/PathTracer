using Microsoft.Extensions.DependencyInjection;
using Pathetic.Renderer;
using Pathetic.Renderer.Graph;
using Pathetic.Renderer.Primitives;
using Pathetic.Renderer.Resources;
using Pathetic.Renderer.Shaders;

namespace Pathetic.Scene.DI;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSceneGraph()
        {
            services.AddSingleton<ComponentFactory>();
            services.AddSingleton<SceneManager>();
            services.AddSingleton<SceneLoop>();
            return services;
        }
    }
}
