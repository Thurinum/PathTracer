using Microsoft.Extensions.DependencyInjection;
using PathTracerCore.Renderer;
using PathTracerCore.Renderer.Passes;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.Renderer.Shaders;
using PathTracerCore.SceneGraph;

namespace PathTracerCore;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEngineCore(
            Action<EngineOptions> configureOptions,
            Action<RenderPassSelector> configurePasses)
        {
            services.Configure(configureOptions);
            
            var passSelector = new RenderPassSelector();
            configurePasses(passSelector);
            services.AddSingleton(passSelector);
            
            if (passSelector.PassType == null)
                throw new InvalidOperationException("No render pass selected");
            
            services.AddSingleton<SlangCompiler>();
            services.AddSingleton<RenderBackend>();
            services.AddSingleton<PrimitiveRegistry>();
            services.AddSingleton<RenderPassFactory>();
            services.AddSingleton<ComponentFactory>();
            services.AddSingleton<SceneManager>();
            services.AddSingleton<SceneLoop>();
            
            return services;
        }
    }
}