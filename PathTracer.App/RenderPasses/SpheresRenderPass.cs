using System.Numerics;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Components;
using PathTracerCore;
using PathTracerCore.Renderer;
using PathTracerCore.Renderer.Passes;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerApp.RenderPasses;

public struct SpheresParams
{
    public Vector3 Center;
    public float Radius;
    public Color Color;
}

public class SpheresRenderPass(
    SlangCompiler compiler,
    IOptions<EngineOptions> options,
    PrimitiveRegistry primitives) : RenderPass(compiler, options)
{
    protected override string ShaderModule => "spheres";
    protected override bool UsesAccumulation => true;

    private DeviceBuffer _paramsBuffer = null!;
    private PrimitiveBuffer<SpherePrimitive> _buffer = null!;

    protected override void SetupResources(GraphicsDevice device)
    {
        _buffer = primitives.Get<SpherePrimitive>();

        _paramsBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription
        {
            SizeInBytes = 32,
            Usage = BufferUsage.UniformBuffer
        });
        
        Register("params", ResourceKind.UniformBuffer, _paramsBuffer);
    }

    protected override void Upload(FrameContext ctx)
    {
        SpheresParams @params = default;

        if (_buffer.Count > 0)
        {
            @params.Center = _buffer.Data[0].Center;
            @params.Radius = _buffer.Data[0].Radius;
            @params.Color = _buffer.Data[0].Color;
        }

        ctx.Cmd.UpdateBuffer(_paramsBuffer, 0, @params);
    }

    protected override void DisposeResources()
    {
        _paramsBuffer.Dispose();
    }
}