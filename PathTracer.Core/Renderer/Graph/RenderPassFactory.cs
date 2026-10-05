using Microsoft.Extensions.DependencyInjection;

namespace PathTracerCore.Renderer.Graph;

public class RenderPassFactory(IServiceProvider provider)
{
    public RenderPass CreateRenderPass(Type type)
    {
        return (RenderPass)ActivatorUtilities.CreateInstance(provider, type);
    }
}
