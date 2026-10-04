using NeoVeldrid;
using PathTracerCore.Renderer.Graph;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerCore.Renderer.Passes;

public abstract class RenderPass(SlangCompiler compiler) : IDisposable
{
    public bool Enabled { get; set; } = true;
    protected abstract string ShaderModule { get; }

    public abstract IReadOnlyList<BindingSpec> Bindings { get; }
    public virtual IReadOnlyList<ResourceRef> Inputs => [];
    public virtual IReadOnlyList<IResourceSpec> Outputs => [];
    public virtual IReadOnlyList<SamplerSpec> Samplers => [];
    
    private Pipeline _pipeline = null!;
    private ResourceSet _resourceSet = null!;
    private (uint x, uint y, uint z) _threadGroupSize;
    
    public void Initialize(GraphicsDevice device, ResourceLayout layout, ResourceSet set)
    {
        ShaderCompilationResult compiled = compiler.CompileComputeShader(ShaderModule);
        _threadGroupSize = compiled.GroupSize;
        ShaderDescription shaderDesc = new(ShaderStages.Compute, compiled.Code, SlangCompiler.EntryPointName);
        Shader shader = device.ResourceFactory.CreateShader(shaderDesc);
        ComputePipelineDescription pipelineDesc = new(shader, layout, _threadGroupSize.x, _threadGroupSize.y, _threadGroupSize.z);
        _pipeline = device.ResourceFactory.CreateComputePipeline(pipelineDesc);
        Rebind(set);
        shader.Dispose();
    }

    internal void Rebind(ResourceSet set) { _resourceSet = set; }
    protected virtual void Upload(RenderContext ctx) {}
    protected virtual (uint x, uint y) DispatchSize(RenderContext ctx) => (ctx.Width, ctx.Height);
    private (uint, uint, uint) ComputeThreadGroupCount(uint width, uint height)
    {
        uint x = (width + _threadGroupSize.x - 1) / _threadGroupSize.x;
        uint y = (height + _threadGroupSize.y - 1) / _threadGroupSize.y;
        return (x, y, 1);
    }

    public void Execute(RenderContext ctx)
    {
        Upload(ctx);
        ctx.Cmd.SetPipeline(_pipeline);
        ctx.Cmd.SetComputeResourceSet(0, _resourceSet);
        (uint w, uint h) = DispatchSize(ctx);
        (uint x, uint y, uint z) = ComputeThreadGroupCount(w, h);
        ctx.Cmd.Dispatch(x, y, z);
    }


    public void Dispose()
    {
        _pipeline.Dispose();
        _resourceSet.Dispose();
    }

    public virtual void DisposeResources() {}
}
