using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerCore.Renderer.Passes;

public record ResourceBinding
{
    public required ResourceLayoutElementDescription Desc;
    public required BindableResource Resource;
}

// TODO: multiple inputs
public abstract class RenderPass(SlangCompiler compiler, IOptions<EngineOptions> options) : IDisposable
{
    private EngineOptions _options = null!;
    private Shader? _shader;
    private Texture? _outputTex;
    private ResourceLayout? _layout;
    private ResourceSet? _set;
    private Pipeline? _pipeline;
    private (uint x, uint y, uint z) _threadGroupSize;
    private readonly List<ResourceBinding> _bindings = [];
    
    protected abstract string ShaderModule { get; }

    protected abstract void SetupResources(GraphicsDevice device);
    protected abstract void Upload(FrameContext ctx);
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
        CompiledShader shader = compiler.CompileComputeShader(ShaderModule);
        ShaderDescription shaderDesc = new(ShaderStages.Compute, shader.Code, SlangCompiler.EntryPointName);
        _shader = device.ResourceFactory.CreateShader(shaderDesc);
        _threadGroupSize = shader.GroupSize;
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
    
    protected void UpdateBinding(string name, BindableResource resource, GraphicsDevice device)
    {
        int i = _bindings.FindIndex(b => b.Desc.Name == name);
        if (i < 0)
        {
            throw new ArgumentException($"No binding named '{name}'.", nameof(name));
        }
        
        _bindings[i].Resource = resource;
        BuildResourceSet(device);
    }

    private void BuildPipeline(GraphicsDevice device)
    {
        ComputePipelineDescription pipelineDesc = new(_shader, _layout, _threadGroupSize.x, _threadGroupSize.y, _threadGroupSize.z); _pipeline = device.ResourceFactory.CreateComputePipeline(pipelineDesc);
    }

    public void Render(FrameContext ctx)
    {
        Upload(ctx);
        
        ctx.Cmd.SetPipeline(_pipeline);
        ctx.Cmd.SetComputeResourceSet(0, _set);
        
        uint groupCountX = (ctx.Target.Width + _threadGroupSize.x - 1) / _threadGroupSize.x;
        uint groupCountY = (ctx.Target.Height + _threadGroupSize.y - 1) / _threadGroupSize.y;
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