using Microsoft.Extensions.DependencyInjection;
using NeoVeldrid;

namespace PathTracerCore.Renderer.Passes;

public class RenderPassFactory(IServiceProvider provider)
{
    public RenderPass CreateRenderPass(Type type, GraphicsDevice device)
    {
        var pass = (RenderPass)ActivatorUtilities.CreateInstance(provider, type);
        pass.Initialize(device);
        return pass;
    }
    
    public RenderPass CreateRenderPass<T>(GraphicsDevice device) where T : RenderPass
    {
        var pass = ActivatorUtilities.CreateInstance<T>(provider);
        pass.Initialize(device);
        return pass;
    }
}