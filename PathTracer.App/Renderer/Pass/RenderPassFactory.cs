using Microsoft.Extensions.DependencyInjection;
using NeoVeldrid;

namespace PathTracerApp.Renderer.Pass;

public class RenderPassFactory(IServiceProvider provider)
{
    public RenderPass CreateRenderPass<T>(GraphicsDevice device) where T : RenderPass
    {
        var pass = ActivatorUtilities.CreateInstance<T>(provider);
        pass.Initialize(device);
        return pass;
    }
}