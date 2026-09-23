using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using NeoVeldrid.ImageSharp;
using PathTracerApp.Components;
using PathTracerCore;
using PathTracerCore.Renderer;
using PathTracerCore.Renderer.Passes;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerApp.RenderPasses;

public struct SpheresParams
{
    public Vector4 Sphere0;
    public Vector4 Sphere1;
    public Vector4 Sphere2;
    public Vector4 Sphere3;
    public Vector4 Albedo0;
    public Vector4 Albedo1;
    public Vector4 Albedo2;
    public Vector4 Albedo3;
    public Vector4 Properties0;
    public Vector4 Properties1;
    public Vector4 Properties2;
    public Vector4 Properties3;
    public uint SphereCount;
    public uint SampleIndex;
    public float Time;
    public float PreviousTime;
}

public class SpheresRenderPass(
    SlangCompiler compiler,
    IOptions<EngineOptions> options,
    PrimitiveRegistry primitives) : RenderPass(compiler, options)
{
    protected override string ShaderModule => "spheres";
    protected override bool UsesAccumulation => true;
    protected override float TemporalHistoryWeight => 0.9f;
    
    private ImageSharpTexture _image = null!;
    private Texture? _env;
    private Texture? _guide;
    private Texture? _motion;
    private DeviceBuffer _paramsBuffer = null!;
    private PrimitiveBuffer<SpherePrimitive> _buffer = null!;
    private uint _sampleIndex;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private float _previousTime;

    protected override void SetupResources(GraphicsDevice device)
    {
        _buffer = primitives.Get<SpherePrimitive>();
        
        string path = Path.Combine(AppContext.BaseDirectory, "Shaders/envmap.webp");
        _image = new ImageSharpTexture(path, mipmap: true);
        _env = _image.CreateDeviceTexture(device, device.ResourceFactory);

        _paramsBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription
        {
            SizeInBytes = (uint)Unsafe.SizeOf<SpheresParams>(),
            Usage = BufferUsage.UniformBuffer
        });
        
        Register("env", ResourceKind.TextureReadOnly, _env);
        Register("params", ResourceKind.UniformBuffer, _paramsBuffer);
        _guide = CreateAuxiliaryTexture(device, Options.WindowWidth, Options.WindowHeight);
        _motion = CreateAuxiliaryTexture(device, Options.WindowWidth, Options.WindowHeight);
        Register("guide", ResourceKind.TextureReadWrite, _guide);
        Register("motion", ResourceKind.TextureReadWrite, _motion);
    }

    protected override void Upload(FrameContext ctx)
    {
        SpheresParams @params = default;
        int count = Math.Min(_buffer.Count, 4);
        for (int i = 0; i < count; i++)
        {
            SpherePrimitive sphere = _buffer.Data[i];
            Vector4 centerRadius = new(sphere.Center, sphere.Radius);
            Vector4 albedoMetallic = new(sphere.Color.R, sphere.Color.G, sphere.Color.B, sphere.Metallic);
            Vector4 materialProperties = new(sphere.Roughness, 0.0f, 0.0f, 0.0f);
            switch (i)
            {
                case 0:
                    @params.Sphere0 = centerRadius;
                    @params.Albedo0 = albedoMetallic;
                    @params.Properties0 = materialProperties;
                    break;
                case 1:
                    @params.Sphere1 = centerRadius;
                    @params.Albedo1 = albedoMetallic;
                    @params.Properties1 = materialProperties;
                    break;
                case 2:
                    @params.Sphere2 = centerRadius;
                    @params.Albedo2 = albedoMetallic;
                    @params.Properties2 = materialProperties;
                    break;
                case 3:
                    @params.Sphere3 = centerRadius;
                    @params.Albedo3 = albedoMetallic;
                    @params.Properties3 = materialProperties;
                    break;
            }
        }

        @params.SphereCount = (uint)count;
        @params.SampleIndex = _sampleIndex++;
        @params.Time = (float)_clock.Elapsed.TotalSeconds;
        @params.PreviousTime = _previousTime;
        _previousTime = @params.Time;
        ctx.Cmd.UpdateBuffer(_paramsBuffer, 0, @params);
    }

    protected override void ResizeResources(GraphicsDevice device, uint width, uint height)
    {
        Texture? oldGuide = _guide;
        Texture? oldMotion = _motion;
        Texture newGuide = CreateAuxiliaryTexture(device, width, height);
        Texture newMotion = CreateAuxiliaryTexture(device, width, height);
        UpdateBinding("guide", newGuide, device);
        UpdateBinding("motion", newMotion, device);
        _guide = newGuide;
        _motion = newMotion;
        oldGuide?.Dispose();
        oldMotion?.Dispose();
    }

    private static Texture CreateAuxiliaryTexture(GraphicsDevice device, uint width, uint height)
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

    protected override void DisposeResources()
    {
        _paramsBuffer.Dispose();
        _guide?.Dispose();
        _motion?.Dispose();
    }
}
