using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using NeoVeldrid.SPIRV;
using PathTracerApp.Shader;

namespace PathTracerApp;

public sealed class PathTracer(IOptions<AppOptions> options, ILogger<PathTracer> logger, SlangCompiler shaderCompiler)
    : IDisposable
{
    private readonly AppOptions _options = options.Value;

    private Texture? _outputTexture;
    private DeviceBuffer? _paramsBuffer;
    private ShaderResources? _shaderResources;
    private ResourceLayout? _resourceLayout;
    private ResourceSet? _resourceSet;
    private Pipeline? _pipeline;

    public float Radius { get; set; } = 200f;

    public void Render(FrameContext frame)
    {
        if (_pipeline is null)
        {
            CreateRenderResources(frame.Device, frame.Width, frame.Height);
        }
        else if (_outputTexture!.Width != frame.Width || _outputTexture.Height != frame.Height)
        {
            ResizeOutput(frame.Device, frame.Width, frame.Height);
        }

        (uint groupX, uint groupY, uint groupZ) = GetThreadGroupSize();

        CommandList commands = frame.CommandList;

        Params parameters = new() { Radius = Radius };
        commands.UpdateBuffer(_paramsBuffer!, 0, ref parameters);

        commands.SetPipeline(_pipeline!);
        ShaderResources.Bind(commands, 0, _resourceSet!, ShaderStages.Compute);

        commands.Dispatch(
            (_outputTexture!.Width + groupX - 1) / groupX,
            (_outputTexture.Height + groupY - 1) / groupY,
            groupZ);

        commands.CopyTexture(_outputTexture, frame.SwapchainTarget);
    }

    private void CreateRenderResources(GraphicsDevice device, uint width, uint height)
    {
        logger.LogInformation("Creating render resources at {Width}x{Height}", width, height);

        _outputTexture = CreateOutputTexture(device, width, height);
        _paramsBuffer = device.ResourceFactory.CreateBuffer(
            new BufferDescription(16, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        byte[] computeCode = shaderCompiler.CompileComputeShader("image");
        NeoVeldrid.Shader computeShader = device.ResourceFactory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Compute, computeCode, "main"));

        _shaderResources = new ShaderResources(device)
            .Add("outputImage", ResourceKind.TextureReadWrite, ShaderStages.Compute)
            .Add("Params", ResourceKind.UniformBuffer, ShaderStages.Compute);

        _resourceLayout = _shaderResources.CreateLayout();
        _resourceSet = _shaderResources.CreateResourceSet(_resourceLayout, _outputTexture, _paramsBuffer);

        (uint groupX, uint groupY, uint groupZ) = GetThreadGroupSize();
        _pipeline = device.ResourceFactory.CreateComputePipeline(
            new ComputePipelineDescription(computeShader, _resourceLayout, groupX, groupY, groupZ));

        computeShader.Dispose();
    }

    private void ResizeOutput(GraphicsDevice device, uint width, uint height)
    {
        logger.LogInformation("Resizing render target to {Width}x{Height}", width, height);

        _resourceSet!.Dispose();
        _outputTexture!.Dispose();

        _outputTexture = CreateOutputTexture(device, width, height);
        _resourceSet = _shaderResources!.CreateResourceSet(_resourceLayout!, _outputTexture, _paramsBuffer!);
    }

    private Texture CreateOutputTexture(GraphicsDevice device, uint width, uint height)
    {
        PixelFormat format = device.SwapchainFramebuffer.ColorTargets[0].Target.Format;
        return device.ResourceFactory.CreateTexture(
            TextureDescription.Texture2D(width, height, 1, 1, format, TextureUsage.Storage));
    }

    private (uint X, uint Y, uint Z) GetThreadGroupSize()
    {
        (int x, int y, int z) = _options.ThreadGroupSize;
        return ((uint)x, (uint)y, (uint)z);
    }

    public void Dispose()
    {
        _pipeline?.Dispose();
        _resourceSet?.Dispose();
        _resourceLayout?.Dispose();
        _paramsBuffer?.Dispose();
        _outputTexture?.Dispose();
    }

    private struct Params
    {
        public float Radius;
    }
}
