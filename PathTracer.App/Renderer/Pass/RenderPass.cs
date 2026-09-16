using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Renderer.Pass;
using PathTracerApp.Shader;

namespace PathTracerApp.Renderer;

public struct Params
{
    public uint Count;
}

public sealed class RenderPass(RenderState state, SlangCompiler compiler, IOptions<RenderBackendOptions> options) : IDisposable
{
    private RenderBackendOptions _options = null!;
    private NeoVeldrid.Shader _shader = null!;
    private Texture? _outputTex;
    private DeviceBuffer _primitiveBuffer = null!;
    private DeviceBuffer _paramsBuffer = null!;
    private ResourceLayout _layout = null!;
    private ResourceSet? _set;
    private Pipeline _pipeline = null!;

    public void Initialize(GraphicsDevice device)
    {
        _options = options.Value;

        byte[] shaderBytes = compiler.CompileComputeShader("image");
        ShaderDescription shaderDesc = new(ShaderStages.Compute, shaderBytes, "main");
        _shader = device.ResourceFactory.CreateShader(shaderDesc);

        BuildOutputTexture(device, _options.WindowWidth, _options.WindowHeight);

        BufferDescription primitiveBufferDesc = new()
        {
            SizeInBytes = (uint)state.Capacity * 16,
            StructureByteStride = 16,
            Usage = BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic
        };
        _primitiveBuffer = device.ResourceFactory.CreateBuffer(primitiveBufferDesc);

        BufferDescription paramsBufferDesc = new()
        {
            SizeInBytes = 16,
            Usage = BufferUsage.UniformBuffer
        };
        _paramsBuffer = device.ResourceFactory.CreateBuffer(paramsBufferDesc);

        ResourceLayoutDescription layoutDesc = new
        (
            new ResourceLayoutElementDescription("outputImage", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("circles", ResourceKind.StructuredBufferReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("params", ResourceKind.UniformBuffer, ShaderStages.Compute)
        );
        
        _layout = device.ResourceFactory.CreateResourceLayout(layoutDesc);
        BuildResourceSet(device);

        (uint g1, uint g2, uint g3) = _options.ThreadGroupSize;
        ComputePipelineDescription pipelineDesc = new(_shader, _layout, g1, g2, g3);
        _pipeline = device.ResourceFactory.CreateComputePipeline(pipelineDesc);
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
        ResourceSetDescription setDesc = new(_layout,
            _outputTex, 
            _primitiveBuffer, 
            _paramsBuffer);
        _set = device.ResourceFactory.CreateResourceSet(setDesc);
    }

    public void Render(FrameContext ctx)
    {
        // todo: delta upload
        Span<GPU_Primitive> primitives = state.State.AsSpan();
        Params @params = new() { Count = (uint)state.Count };
        
        ctx.Cmd.UpdateBuffer(_primitiveBuffer, 0, primitives);
        ctx.Cmd.UpdateBuffer(_paramsBuffer, 0, @params);
        
        ctx.Cmd.SetPipeline(_pipeline);
        ctx.Cmd.SetComputeResourceSet(0, _set);

        uint groupCountX = (ctx.Target.Width + 7) / 8;
        uint groupCountY = (ctx.Target.Height + 7) / 8;
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
        _outputTex?.Dispose();
        _shader.Dispose();
        _primitiveBuffer.Dispose();
        _paramsBuffer.Dispose();
        _layout.Dispose();
        _set?.Dispose();
        _pipeline.Dispose();
    }
}