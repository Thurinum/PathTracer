using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerCore.Renderer.Passes;

public record ResourceBinding
{
    public required ResourceLayoutElementDescription Desc;
    public BindableResource? Resource;
    public int InputIndex { get; init; } = -1;
    public bool IsInput => InputIndex >= 0;
}

public abstract class RenderPass(SlangCompiler compiler, IOptions<EngineOptions> options) : IDisposable
{
    public virtual string[] Inputs { get; } = [];
    internal string OutputId => GetType().Name;
    public Texture[]? BoundInputs { get; internal set; }
    public Texture? Output { get; internal set; }
    
    private Shader? _shader;
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

    protected void RegisterInput(string name, string inputId, ResourceKind kind = ResourceKind.TextureReadOnly)
    {
        int index = Array.IndexOf(Inputs, inputId);
        if (index < 0)
            throw new ArgumentException($"Input '{inputId}' is not declared in Inputs.", nameof(inputId));

        _bindings.Add(new ResourceBinding
        {
            Desc = new ResourceLayoutElementDescription(name, kind, ShaderStages.Compute),
            InputIndex = index
        });
    }

    public void Initialize(GraphicsDevice device)
    {
        if (Output is null)
            throw new InvalidOperationException($"Pass '{OutputId}' has no output texture bound.");

        _bindings.Clear();

        CompileShader(device);
        SetupResources(device);
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

    private void BuildResourceSet(GraphicsDevice device)
    {
        _set?.Dispose();
         
        var elements = _bindings
            .Select(ResolveBinding)
            .Prepend(Output!)
            .ToArray();
        
        ResourceSetDescription setDesc = new(_layout, elements);
        _set = device.ResourceFactory.CreateResourceSet(setDesc);
    }

    private BindableResource ResolveBinding(ResourceBinding binding)
    {
        if (!binding.IsInput)
            return binding.Resource ?? throw new InvalidOperationException($"Binding '{binding.Desc.Name}' has no resource.");

        if (BoundInputs is null)
            throw new InvalidOperationException($"Binding '{binding.Desc.Name}' needs input '{Inputs[binding.InputIndex]}' but inputs were not bound.");

        return BoundInputs[binding.InputIndex];
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
        
        uint groupCountX = (Output!.Width + _threadGroupSize.x - 1) / _threadGroupSize.x;
        uint groupCountY = (Output.Height + _threadGroupSize.y - 1) / _threadGroupSize.y;
        ctx.Cmd.Dispatch(groupCountX, groupCountY, 1);
        
        ctx.Cmd.CopyTexture(Output, ctx.Target);
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        BuildResourceSet(device);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        
        _shader?.Dispose();
        DisposeResources();
        _set?.Dispose();
        _layout?.Dispose();
        _pipeline?.Dispose();
    }
}
