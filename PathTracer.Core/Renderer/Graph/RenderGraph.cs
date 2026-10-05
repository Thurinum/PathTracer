using NeoVeldrid;
using PathTracerCore.Renderer.Resources;

namespace PathTracerCore.Renderer.Graph;

public class PassEntry(RenderPass pass)
{
    public RenderPass Pass = pass;
    public PassRenderState? State;
}

public sealed class RenderGraph(
    RenderGraphBuilder graphBuilder,
    RenderPassFactory passFactory,
    RenderGraphPipelineFactory stateFactory,
    SceneRevision sceneRevision,
    IEnumerable<IResourceProvider> resourceProviders) : IDisposable
{
    private readonly ResourceTable _resources = new();
    private readonly List<PassEntry> _passes = [];
    private uint _frameIndex = 0;

    public T? GetPass<T>() where T : RenderPass =>
        _passes.Select(e => e.Pass).OfType<T>().SingleOrDefault();

    public void Build(GraphicsDevice device)
    {
        // register all passes
        foreach (var passType in graphBuilder.PassTypes)
        {
            var pass = passFactory.CreateRenderPass(passType);
            _passes.Add(new PassEntry(pass));
        }

        // declare global resources (camera, lights, etc.)
        foreach (var spec in resourceProviders.SelectMany(p => p.Resources))
        {
            _resources.Declare(spec);
        }
        
        // declare pass outputs and samplers
        foreach (var entry in _passes)
        {
            foreach (var output in entry.Pass.Outputs)
            {
                _resources.Declare(output);
            }
            foreach (var sampler in entry.Pass.Samplers)
            {
                _resources.Declare(sampler);
            }
        }

        // create declared resources
        var framebuffer = device.SwapchainFramebuffer;
        _resources.Allocate(device, framebuffer.Width, framebuffer.Height);
        
        // check for a valid present texture
        // TODO: can be simplified, Has() could be made spec only
        if (graphBuilder.PresentTextureName == null)
            throw new InvalidOperationException("No present texture is set.");
        
        if (!_resources.Has<Texture>(graphBuilder.PresentTextureName))
            throw new InvalidOperationException($"Present texture {graphBuilder.PresentTextureName} isn't a valid resource.");
        
        if (_resources.GetTexture(graphBuilder.PresentTextureName).Format != framebuffer.ColorTargets[0].Target.Format)
            throw new InvalidOperationException($"Present tex {graphBuilder.PresentTextureName} must have same format as the swapchain.");
        
        // check that inputs are valid
        foreach (var entry in _passes)
        {
            foreach (var input in entry.Pass.Inputs)
            {
                if (!_resources.Has(input.Name))
                {
                    throw new InvalidOperationException($"Pass {entry.Pass.GetType().Name} has invalid input {input.Name}");
                }
            }
        }

        // build each pass's pipeline using the resources
        foreach (var entry in _passes)
        {
            entry.State = stateFactory.Build(device, entry.Pass, _resources);
        }
    }

    public void Render(GraphicsDevice device, CommandList cmd, float deltaTime)
    {
        var target = device.SwapchainFramebuffer.ColorTargets[0].Target;
        RenderContext ctx = new()
        {
            Cmd = cmd,
            Resources = _resources,
            Target = target,
            Width = target.Width,
            Height = target.Height,
            DeltaTime = deltaTime,
            FrameIndex = _frameIndex,
            SceneRevision = sceneRevision.Value
        };
        
        foreach (var provider in resourceProviders)
        {
            provider.Update(ctx);
        }

        // TODO: Only update passes whose dependencies were reallocated
        IReadOnlyList<RenderGraphResource> reallocated = _resources.Refresh(device, ctx.Cmd);
        if (reallocated.Count > 0)
        {
            foreach (var entry in _passes)
            {
                stateFactory.UpdateResourceSet(entry.State!, device, _resources);
            }
        }
        
        foreach (var entry in _passes)
        {
            if (entry.Pass.Enabled)
            {
                entry.Pass.Execute(ctx, entry.State!);
            }
        }

        var presentTexture = _resources.GetTexture(graphBuilder.PresentTextureName!);
        ctx.Cmd.CopyTexture(presentTexture, ctx.Target);

        _frameIndex++;
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        sceneRevision.Invalidate();
        _resources.Resize(device, width, height);
        
        foreach (var entry in _passes)
        {
            stateFactory.UpdateResourceSet(entry.State!, device, _resources);
        }
    }

    public void Dispose()
    {
        foreach (var entry in _passes)
        {
            entry.State?.Dispose();
        }
        
        _resources.Dispose();
    }
}
