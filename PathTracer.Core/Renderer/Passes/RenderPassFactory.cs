using Microsoft.Extensions.DependencyInjection;

namespace PathTracerCore.Renderer.Passes;

public class RenderPassFactory(IServiceProvider provider)
{
    public RenderPass CreateRenderPass(Type type)
    {
        return (RenderPass)ActivatorUtilities.CreateInstance(provider, type);
    }
}
