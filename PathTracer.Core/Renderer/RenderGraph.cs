using NeoVeldrid;
using PathTracerCore.Renderer.Passes;

namespace PathTracerCore.Renderer;

public class RenderGraph(RenderPassFactory factory, RenderPassSelector passSelector)
{
    private readonly List<RenderPass> _passes = [];

    public void AddPasses(GraphicsDevice device)
    {
        foreach (var passType in passSelector.PassTypes)
        {
            _passes.Add(factory.CreateRenderPass(passType, device));
        }
    }

    public void Render(FrameContext ctx)
    {
        foreach (var pass in _passes)
        {
            pass.Render(ctx);
        }
    }
    
    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        foreach (var pass in _passes)
        {
            pass.Resize(device, width, height);
        }
    }
    
    public void Dispose()
    {
        foreach (var pass in _passes)
        {
            pass.Dispose();
        }
    }
}