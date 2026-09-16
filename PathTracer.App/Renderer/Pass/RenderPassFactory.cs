using Microsoft.Extensions.DependencyInjection;
using NeoVeldrid;

namespace PathTracerApp.Renderer.Pass;

public class RenderPassFactory(IServiceProvider provider)
{
    public RenderPass CreateRenderPass(GraphicsDevice device)
    {
        var pass = ActivatorUtilities.CreateInstance<RenderPass>(provider);
        pass.Initialize(device);
        return pass;
    }
}