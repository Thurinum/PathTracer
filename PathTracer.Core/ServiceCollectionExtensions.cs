using Microsoft.Extensions.DependencyInjection;
using PathTracerCore.Renderer;
using PathTracerCore.Renderer.Camera;
using PathTracerCore.Renderer.Graph;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.Renderer.Resources;
using PathTracerCore.SceneGraph;

namespace PathTracerCore;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        private void AddResourceProvider<T>() where T : class, IResourceProvider
        {
            services.AddSingleton<T>();
            services.AddSingleton<IResourceProvider>(sp => sp.GetRequiredService<T>());
        }
        
        public IServiceCollection AddEngineCore(
            Action<EngineOptions> configureOptions,
            Action<RenderGraphBuilder> configurePasses,
            Action<PrimitiveRegistry>? configurePrimitives = null)
        {
            services.Configure(configureOptions);
            
            var passSelector = new RenderGraphBuilder();
            configurePasses(passSelector);
            services.AddSingleton(passSelector);
            
            if (passSelector.PassTypes.Count == 0)
                throw new InvalidOperationException("No render pass selected");

            var primitives = new PrimitiveRegistry();
            configurePrimitives?.Invoke(primitives);
            services.AddSingleton(primitives);
            services.AddSingleton<IResourceProvider>(primitives);
            
            services.AddSingleton<SlangCompiler>();
            services.AddSingleton<RenderPassFactory>();
            services.AddSingleton<RenderGraphPipelineFactory>();
            services.AddSingleton<RenderGraph>();
            services.AddSingleton<RenderBackend>();
            services.AddSingleton<SceneRevision>();

            services.AddResourceProvider<CameraProvider>();
            
            services.AddSingleton<ComponentFactory>();
            services.AddSingleton<SceneManager>();
            services.AddSingleton<SceneLoop>();
            
            return services;
        }
    }
}
