using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerCore;
using PathTracerCore.Renderer;
using PathTracerCore.Renderer.Camera;
using PathTracerCore.Renderer.Passes;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerApp.RenderPasses;

public class AccumulationPass(SlangCompiler compiler, CameraState camera, IOptions<EngineOptions> options) : RenderPass(compiler, options)
{
    private struct Params
    {
        public uint Reset;
    }
    
    public override string[] Inputs { get; } = [nameof(PathTracerPass)];

    protected override string ShaderModule => "Accumulation";

    private Texture? _accumulatedTex;
    private bool _needsReset = true;
    private DeviceBuffer? _paramsBuffer;
    private uint _cameraVersion = 0;

    protected override void SetupResources(GraphicsDevice device)
    {
        _paramsBuffer = device.ResourceFactory.CreateBuffer(
            new BufferDescription { SizeInBytes = 16, Usage = BufferUsage.UniformBuffer });

        _accumulatedTex = BuildAccumulatedTexture(device);

        RegisterInput("sampleImage", Inputs[0]);
        Register("accumImage", ResourceKind.TextureReadWrite, _accumulatedTex);
        Register("params", ResourceKind.UniformBuffer, _paramsBuffer);
    }

    protected override void Upload(FrameContext ctx)
    {
        ctx.Cmd.UpdateBuffer(_paramsBuffer!, 0, new Params { Reset = Convert.ToUInt32(_needsReset) });
        _needsReset = false;
        
        if (camera.Version != _cameraVersion)
        {
            _cameraVersion = camera.Version;
            _needsReset = true;
        }
    }

    public override void Resize(GraphicsDevice device, uint width, uint height)
    {
        _accumulatedTex?.Dispose();
        _accumulatedTex = BuildAccumulatedTexture(device);
        UpdateBinding("accumImage", _accumulatedTex, device);
        _needsReset = true;
    }

    protected override void DisposeResources()
    {
        _accumulatedTex?.Dispose();
        _paramsBuffer?.Dispose();
    }

    private Texture BuildAccumulatedTexture(GraphicsDevice device)
    {
        return device.ResourceFactory.CreateTexture(new TextureDescription
        {
            Width = Output!.Width, Height = Output.Height,
            Depth = 1, MipLevels = 1, ArrayLayers = 1,
            Format = PixelFormat.R32_G32_B32_A32_Float,
            Usage = TextureUsage.Storage,
            Type = TextureType.Texture2D,
            SampleCount = TextureSampleCount.Count1
        });
    }
}
