using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Shader;

namespace PathTracerApp.Renderer.Pass;

public record ResourceBinding
{
    public required ResourceLayoutElementDescription Desc;
    public required BindableResource Resource;
}

// TODO: multiple inputs
public abstract class RenderPass(SlangCompiler compiler, IOptions<RenderBackendOptions> options) : IDisposable
{
    private RenderBackendOptions _options = null!;
    private NeoVeldrid.Shader? _shader;
    private Texture? _outputTex;
    private ResourceLayout? _layout;
    private ResourceSet? _set;
    private Pipeline? _pipeline;
    private readonly List<ResourceBinding> _bindings = [];
    
    protected abstract string ShaderModule { get; }

    protected abstract void SetupResources(GraphicsDevice device);
    protected abstract void Upload(CommandList cmd);
    protected abstract void DisposeResources();

    protected void Register(string name, ResourceKind kind, BindableResource resource)
    {
        _bindings.Add(new ResourceBinding
        {
            Desc = new ResourceLayoutElementDescription(name, kind, ShaderStages.Compute),
            Resource = resource
        });
    }

    public void Initialize(GraphicsDevice device)
    {
        _options = options.Value;
        _bindings.Clear();

        CompileShader(device);
        SetupResources(device);
        BuildOutputTexture(device, _options.WindowWidth, _options.WindowHeight);
        BuildLayout(device);
        BuildResourceSet(device);
        BuildPipeline(device);
    }

    private void BuildLayout(GraphicsDevice device)
    {
        var outputDesc =
            new ResourceLayoutElementDescription("outputImage", ResourceKind.TextureReadWrite, ShaderStages.Compute);
        var elements = _bindings.Select(b => b.Desc)
            .Prepend(outputDesc)
            .ToArray();
        ResourceLayoutDescription layoutDesc = new(elements);
        _layout = device.ResourceFactory.CreateResourceLayout(layoutDesc);
    }

    private void CompileShader(GraphicsDevice device)
    {
        byte[] shaderBytes = compiler.CompileComputeShader(ShaderModule);
        ShaderDescription shaderDesc = new(ShaderStages.Compute, shaderBytes, "main");
        _shader = device.ResourceFactory.CreateShader(shaderDesc);
    }

    private void BuildOutputTexture(GraphicsDevice device, uint width, uint height)
    {
        _outputTex?.Dispose();
        TextureDescription colorTargetDesc = new()
        {
            Width = width,
            Height = height,
            Depth = 1,
            MipLevels = 1,
            ArrayLayers = 1,
            Format = device.SwapchainFramebuffer.OutputDescription.ColorAttachments[0].Format,
            Usage = TextureUsage.Sampled | TextureUsage.Storage,
            Type = TextureType.Texture2D,
            SampleCount = TextureSampleCount.Count1
        };
        _outputTex = device.ResourceFactory.CreateTexture(colorTargetDesc);
    }

    private void BuildResourceSet(GraphicsDevice device)
    {
        _set?.Dispose();

        var elements = _bindings
            .Select(b => b.Resource)
            .Prepend(_outputTex)
            .ToArray();
        ResourceSetDescription setDesc = new(_layout, elements);
        _set = device.ResourceFactory.CreateResourceSet(setDesc);
    }

    private void BuildPipeline(GraphicsDevice device)
    {
        (uint g1, uint g2, uint g3) = _options.ThreadGroupSize;
        ComputePipelineDescription pipelineDesc = new(_shader, _layout, g1, g2, g3);
        _pipeline = device.ResourceFactory.CreateComputePipeline(pipelineDesc);
    }

    public void Render(FrameContext ctx)
    {
        Upload(ctx.Cmd);
        
        ctx.Cmd.SetPipeline(_pipeline);
        ctx.Cmd.SetComputeResourceSet(0, _set);
        
        var (g1, g2, _) = _options.ThreadGroupSize;
        uint groupCountX = (ctx.Target.Width + g1 - 1) / g1;
        uint groupCountY = (ctx.Target.Height + g2 - 1) / g2;
        ctx.Cmd.Dispatch(groupCountX, groupCountY, 1);
        
        ctx.Cmd.CopyTexture(_outputTex, ctx.Target);
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        BuildOutputTexture(device, width, height);
        BuildResourceSet(device);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        _outputTex?.Dispose();
        _shader?.Dispose();
        DisposeResources();
        _set?.Dispose();
        _layout?.Dispose();
        _pipeline?.Dispose();
    }
}