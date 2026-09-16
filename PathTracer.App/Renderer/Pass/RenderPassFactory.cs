using Microsoft.Extensions.DependencyInjection;
using NeoVeldrid;

namespace PathTracerApp.Renderer.Pass;

public class RenderPassFactory(IServiceProvider provider)
{
    public RenderPassScaffold CreateRenderPass(GraphicsDevice device)
    {
        var pass = ActivatorUtilities.CreateInstance<RenderPassScaffold>(provider);
        pass.Initialize(device);
        return pass;
    }
}