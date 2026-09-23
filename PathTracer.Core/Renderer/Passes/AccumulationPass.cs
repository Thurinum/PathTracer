using NeoVeldrid;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerCore.Renderer.Passes;

public struct AccumulationParams
{
    public uint Reset;
}

public class AccumulationPass(SlangCompiler compiler) : IDisposable
{
    private const string ShaderModule = "accumulate";
    private Texture? _accumulatedTex;
    private ResourceLayout? _layout;
    private bool _needsReset = true;
    private DeviceBuffer? _paramsBuffer;
    private Pipeline? _pipeline;
    private ResourceSet? _set;
    private Shader? _shader;
    private (uint x, uint y, uint z) _threadGroupSize;


    public void Dispose()
    {
        _accumulatedTex?.Dispose();
        _paramsBuffer?.Dispose();
        _set?.Dispose();
        _layout?.Dispose();
        _pipeline?.Dispose();
        _shader?.Dispose();
    }

    public void Initialize(GraphicsDevice device, Texture sample, Texture output)
    {
        var shader = compiler.CompileComputeShader(ShaderModule);
        ShaderDescription shaderDesc = new(ShaderStages.Compute, shader.Code, SlangCompiler.EntryPointName);
        _shader = device.ResourceFactory.CreateShader(shaderDesc);
        _threadGroupSize = shader.GroupSize;
        _paramsBuffer = device.ResourceFactory.CreateBuffer(
            new BufferDescription { SizeInBytes = 16, Usage = BufferUsage.UniformBuffer });

        _layout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("sampleImage", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("accumImage", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("outputImage", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("params", ResourceKind.UniformBuffer, ShaderStages.Compute)));

        _pipeline = device.ResourceFactory.CreateComputePipeline(
            new ComputePipelineDescription(_shader, _layout, _threadGroupSize.x, _threadGroupSize.y,
                _threadGroupSize.z));

        Resize(device, sample, output);
    }

    public void Prepare(CommandList cmd)
    {
        cmd.UpdateBuffer(_paramsBuffer, 0, new AccumulationParams { Reset = Convert.ToUInt32(_needsReset) });
        _needsReset = false;
    }

    public void Dispatch(CommandList cmd, uint width, uint height)
    {
        cmd.SetPipeline(_pipeline);
        cmd.SetComputeResourceSet(0, _set);
        cmd.Dispatch(
            (width + _threadGroupSize.x - 1) / _threadGroupSize.x,
            (height + _threadGroupSize.y - 1) / _threadGroupSize.y,
            1);
    }

    public void Resize(GraphicsDevice device, Texture sample, Texture output)
    {
        _accumulatedTex?.Dispose();

        _accumulatedTex = device.ResourceFactory.CreateTexture(new TextureDescription
        {
            Width = output.Width, Height = output.Height,
            Depth = 1, MipLevels = 1, ArrayLayers = 1,
            Format = PixelFormat.R32_G32_B32_A32_Float,
            Usage = TextureUsage.Storage,
            Type = TextureType.Texture2D,
            SampleCount = TextureSampleCount.Count1
        });

        _set?.Dispose();
        _set = device.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(_layout, sample, _accumulatedTex, output, _paramsBuffer));

        _needsReset = true;
    }
}