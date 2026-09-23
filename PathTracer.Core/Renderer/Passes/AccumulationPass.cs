using NeoVeldrid;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerCore.Renderer.Passes;

public struct AccumulationParams
{
    public uint Reset;
    public float HistoryWeight;
    public uint Pad0;
    public uint Pad1;
}

public struct AtrousParams
{
    public uint StepSize;
    public uint FinalPass;
    public uint Pad0;
    public uint Pad1;
}

public class AccumulationPass(SlangCompiler compiler) : IDisposable
{
    private const string AccumulationShader = "accumulate";
    private const string AtrousShader = "atrous";

    private Texture? _historyA;
    private Texture? _historyB;
    private Texture? _guideHistoryA;
    private Texture? _guideHistoryB;
    private Texture? _scratchA;
    private Texture? _scratchB;

    private ResourceLayout? _layout;
    private ResourceLayout? _atrousLayout;
    private bool _readFromA = true;
    private bool _needsReset = true;
    private DeviceBuffer? _paramsBuffer;
    private DeviceBuffer? _atrousParamsBuffer;
    private Pipeline? _pipeline;
    private Pipeline? _atrousPipeline;
    private ResourceSet? _setA;
    private ResourceSet? _setB;
    private ResourceSet? _atrousHistoryA;
    private ResourceSet? _atrousHistoryB;
    private ResourceSet? _atrousScratchA;
    private ResourceSet? _atrousScratchB;
    private Shader? _shader;
    private Shader? _atrousShader;
    private (uint x, uint y, uint z) _threadGroupSize;
    private (uint x, uint y, uint z) _atrousThreadGroupSize;

    public void Dispose()
    {
        DisposeSets();
        DisposeTextures();
        _paramsBuffer?.Dispose();
        _atrousParamsBuffer?.Dispose();
        _layout?.Dispose();
        _atrousLayout?.Dispose();
        _pipeline?.Dispose();
        _atrousPipeline?.Dispose();
        _shader?.Dispose();
        _atrousShader?.Dispose();
    }

    private void DisposeTextures()
    {
        _historyA?.Dispose();
        _historyB?.Dispose();
        _guideHistoryA?.Dispose();
        _guideHistoryB?.Dispose();
        _scratchA?.Dispose();
        _scratchB?.Dispose();
    }

    private void DisposeSets()
    {
        _setA?.Dispose();
        _setB?.Dispose();
        _atrousHistoryA?.Dispose();
        _atrousHistoryB?.Dispose();
        _atrousScratchA?.Dispose();
        _atrousScratchB?.Dispose();
    }

    public void Initialize(GraphicsDevice device, Texture sample, Texture guide, Texture motion, Texture output)
    {
        CompiledShader accumulation = compiler.CompileComputeShader(AccumulationShader);
        _shader = device.ResourceFactory.CreateShader(
            new ShaderDescription(ShaderStages.Compute, accumulation.Code, SlangCompiler.EntryPointName));
        _threadGroupSize = accumulation.GroupSize;
        _paramsBuffer = device.ResourceFactory.CreateBuffer(
            new BufferDescription { SizeInBytes = 16, Usage = BufferUsage.UniformBuffer });

        _layout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("sampleImage", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("historyImage", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("guideImage", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("motionImage", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("guideHistoryImage", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("historyOutImage", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("guideHistoryOutImage", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("params", ResourceKind.UniformBuffer, ShaderStages.Compute)));
        _pipeline = device.ResourceFactory.CreateComputePipeline(
            new ComputePipelineDescription(_shader, _layout, _threadGroupSize.x, _threadGroupSize.y,
                _threadGroupSize.z));

        CompiledShader atrous = compiler.CompileComputeShader(AtrousShader);
        _atrousShader = device.ResourceFactory.CreateShader(
            new ShaderDescription(ShaderStages.Compute, atrous.Code, SlangCompiler.EntryPointName));
        _atrousThreadGroupSize = atrous.GroupSize;
        _atrousParamsBuffer = device.ResourceFactory.CreateBuffer(
            new BufferDescription { SizeInBytes = 16, Usage = BufferUsage.UniformBuffer });
        _atrousLayout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("sourceImage", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("guideImage", ResourceKind.TextureReadOnly, ShaderStages.Compute),
            new ResourceLayoutElementDescription("destinationImage", ResourceKind.TextureReadWrite, ShaderStages.Compute),
            new ResourceLayoutElementDescription("params", ResourceKind.UniformBuffer, ShaderStages.Compute)));
        _atrousPipeline = device.ResourceFactory.CreateComputePipeline(
            new ComputePipelineDescription(_atrousShader, _atrousLayout, _atrousThreadGroupSize.x,
                _atrousThreadGroupSize.y, _atrousThreadGroupSize.z));

        Resize(device, sample, guide, motion, output);
    }

    public void Prepare(CommandList cmd, float historyWeight)
    {
        cmd.UpdateBuffer(_paramsBuffer, 0, new AccumulationParams
        {
            Reset = Convert.ToUInt32(_needsReset),
            HistoryWeight = Math.Clamp(historyWeight, 0.0f, 0.99f)
        });
        _needsReset = false;
    }

    public void Dispatch(CommandList cmd, uint width, uint height)
    {
        bool writeHistoryA = !_readFromA;
        cmd.SetPipeline(_pipeline);
        cmd.SetComputeResourceSet(0, _readFromA ? _setA : _setB);
        DispatchGroups(cmd, width, height, _threadGroupSize);
        _readFromA = !_readFromA;

        cmd.SetPipeline(_atrousPipeline);
        cmd.SetComputeResourceSet(0, writeHistoryA ? _atrousHistoryA : _atrousHistoryB);
        DispatchAtrous(cmd, width, height, stepSize: 1, finalPass: false);

        cmd.SetComputeResourceSet(0, _atrousScratchA);
        DispatchAtrous(cmd, width, height, stepSize: 2, finalPass: false);

        cmd.SetComputeResourceSet(0, _atrousScratchB);
        DispatchAtrous(cmd, width, height, stepSize: 4, finalPass: true);
    }

    public void Resize(GraphicsDevice device, Texture sample, Texture guide, Texture motion, Texture output)
    {
        DisposeSets();
        DisposeTextures();
        _historyA = CreateHistoryTexture(device, output.Width, output.Height);
        _historyB = CreateHistoryTexture(device, output.Width, output.Height);
        _guideHistoryA = CreateHistoryTexture(device, output.Width, output.Height);
        _guideHistoryB = CreateHistoryTexture(device, output.Width, output.Height);
        _scratchA = CreateHistoryTexture(device, output.Width, output.Height);
        _scratchB = CreateHistoryTexture(device, output.Width, output.Height);

        _setA = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _layout, sample, _historyA, guide, motion, _guideHistoryA, _historyB, _guideHistoryB, _paramsBuffer));
        _setB = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _layout, sample, _historyB, guide, motion, _guideHistoryB, _historyA, _guideHistoryA, _paramsBuffer));

        // These two sets select the freshly written temporal history as the source of wavelet step 1.
        _atrousHistoryA = device.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(_atrousLayout, _historyA, guide, _scratchA, _atrousParamsBuffer));
        _atrousHistoryB = device.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(_atrousLayout, _historyB, guide, _scratchA, _atrousParamsBuffer));
        _atrousScratchA = device.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(_atrousLayout, _scratchA, guide, _scratchB, _atrousParamsBuffer));
        _atrousScratchB = device.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(_atrousLayout, _scratchB, guide, output, _atrousParamsBuffer));

        _readFromA = true;
        _needsReset = true;
    }

    private void DispatchAtrous(CommandList cmd, uint width, uint height, uint stepSize, bool finalPass)
    {
        cmd.UpdateBuffer(_atrousParamsBuffer, 0, new AtrousParams
        {
            StepSize = stepSize,
            FinalPass = Convert.ToUInt32(finalPass)
        });
        DispatchGroups(cmd, width, height, _atrousThreadGroupSize);
    }

    private static void DispatchGroups(CommandList cmd, uint width, uint height, (uint x, uint y, uint z) groupSize)
    {
        cmd.Dispatch(
            (width + groupSize.x - 1) / groupSize.x,
            (height + groupSize.y - 1) / groupSize.y,
            1);
    }

    private static Texture CreateHistoryTexture(GraphicsDevice device, uint width, uint height)
    {
        return device.ResourceFactory.CreateTexture(new TextureDescription
        {
            Width = width, Height = height,
            Depth = 1, MipLevels = 1, ArrayLayers = 1,
            Format = PixelFormat.R32_G32_B32_A32_Float,
            Usage = TextureUsage.Sampled | TextureUsage.Storage,
            Type = TextureType.Texture2D,
            SampleCount = TextureSampleCount.Count1
        });
    }
}
