using NeoVeldrid;

namespace PathTracerCore.Renderer.Passes;

public struct SharedTextureDesc()
{
    public string Id;
    public TextureDescription Desc;
    public bool AutoSize = true;
}

public class RenderGraph(RenderPassFactory factory, RenderGraphBuilder graphBuilder)
{
    private sealed class SharedTextureEntry
    {
        public required string Id;
        public required TextureDescription Desc;
        public required bool AutoSize;
        public Texture Texture = null!;
    }

    private readonly List<RenderPass> _passes = [];
    private readonly Dictionary<string, SharedTextureEntry> _sharedTextures = [];

    public void AddPasses(GraphicsDevice device)
    {
        foreach (var passType in graphBuilder.PassTypes)
        {
            _passes.Add(factory.CreateRenderPass(passType));
        }

        var framebuffer = device.MainSwapchain.Framebuffer;
        AllocSharedTextures(device, framebuffer.Width, framebuffer.Height);

        foreach (var pass in _passes)
        {
            BindPassTextures(pass);
            pass.Initialize(device);
        }
    }

    public void Render(FrameContext ctx)
    {
        foreach (var pass in _passes)
        {
            pass.Render(ctx);
        }
        
        ctx.Cmd.CopyTexture(_passes[^1].Output, ctx.Target);
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        ResizeSharedTextures(device, width, height);

        foreach (var pass in _passes)
        {
            BindPassTextures(pass);
            pass.Resize(device, width, height);
        }
    }

    public void Dispose()
    {
        foreach (var pass in _passes)
        {
            pass.Dispose();
        }

        foreach (var entry in _sharedTextures.Values)
        {
            entry.Texture.Dispose();
        }
        _sharedTextures.Clear();
    }

    private void AllocSharedTextures(GraphicsDevice device, uint width, uint height)
    {
        var format = device.MainSwapchain.Framebuffer.OutputDescription.ColorAttachments[0].Format;

        foreach (var sharedTex in graphBuilder.SharedTextures)
        {
            AddSharedTexture(device, sharedTex.Id, sharedTex.Desc, sharedTex.AutoSize, width, height);
        }

        foreach (var pass in _passes)
        {
            if (_sharedTextures.ContainsKey(pass.OutputId))
                continue;

            AddSharedTexture(device, pass.OutputId, DefaultOutputDesc(format), true, width, height);
        }
    }

    private void ResizeSharedTextures(GraphicsDevice device, uint width, uint height)
    {
        foreach (var entry in _sharedTextures.Values)
        {
            if (!entry.AutoSize)
                continue;

            entry.Texture.Dispose();
            entry.Texture = CreateTexture(device, entry, width, height);
        }
    }

    private void AddSharedTexture(
        GraphicsDevice device, string id, TextureDescription desc, bool autoSize, uint width, uint height)
    {
        if (_sharedTextures.ContainsKey(id))
            throw new InvalidOperationException($"Shared texture '{id}' was declared more than once.");

        var entry = new SharedTextureEntry
        {
            Id = id,
            Desc = desc,
            AutoSize = autoSize
        };
        entry.Texture = CreateTexture(device, entry, width, height);
        _sharedTextures[id] = entry;
    }

    private static Texture CreateTexture(GraphicsDevice device, SharedTextureEntry entry, uint width, uint height)
    {
        TextureDescription desc = entry.AutoSize
            ? entry.Desc with { Width = width, Height = height }
            : entry.Desc;

        return device.ResourceFactory.CreateTexture(desc);
    }

    private void BindPassTextures(RenderPass pass)
    {
        pass.BoundInputs = new Texture[pass.Inputs.Length];

        for (int i = 0; i < pass.Inputs.Length; i++)
        {
            string id = pass.Inputs[i];
            if (!_sharedTextures.TryGetValue(id, out SharedTextureEntry? entry))
                throw new InvalidOperationException($"Shared texture '{id}' was not allocated!");

            pass.BoundInputs[i] = entry.Texture;
        }

        if (!_sharedTextures.TryGetValue(pass.OutputId, out SharedTextureEntry? output))
            throw new InvalidOperationException($"Shared output '{pass.OutputId}' was not allocated!");

        pass.Output = output.Texture;
    }

    private static TextureDescription DefaultOutputDesc(PixelFormat format) => new()
    {
        Width = 1,
        Height = 1,
        Depth = 1,
        MipLevels = 1,
        ArrayLayers = 1,
        Format = format,
        Usage = TextureUsage.Sampled | TextureUsage.Storage,
        Type = TextureType.Texture2D,
        SampleCount = TextureSampleCount.Count1
    };
}
