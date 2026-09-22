using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Components;
using PathTracerCore;
using PathTracerCore.Renderer;
using PathTracerCore.Renderer.Camera;
using PathTracerCore.Renderer.Passes;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerApp.RenderPasses;


public class PlanesRenderPass(CameraState view, ILogger<PlanesRenderPass> logger, SlangCompiler compiler, IOptions<EngineOptions> options, PrimitiveRegistry primitives) : RenderPass(compiler, options)
{
    private struct Params
    {
        public uint Count;
    }
    
    protected override string ShaderModule => "planes";

    public override string[] Inputs { get; } = [nameof(CirclesRenderPass)];

    private Sampler _sampler;
    private DeviceBuffer _cameraBuffer = null!;
    private DeviceBuffer _primitiveBuffer = null!;
    private DeviceBuffer _paramsBuffer = null!;
    private PrimitiveBuffer<PlanePrimitive> _buffer = null!;
    private int _bufferVersion = 0;
    
    protected override void SetupResources(GraphicsDevice device)
    {
        _buffer = primitives.Get<PlanePrimitive>();

        _sampler = device.ResourceFactory.CreateSampler(SamplerDescription.Linear);
        RegisterInput("circles", nameof(CirclesRenderPass));
        Register("circlesSample", ResourceKind.Sampler, _sampler);

        BufferDescription cameraDesc = new()
        {
            SizeInBytes = (uint)Unsafe.SizeOf<CameraData>(),
            Usage = BufferUsage.UniformBuffer
        };
        _cameraBuffer = device.ResourceFactory.CreateBuffer(cameraDesc);
        Register("camera", ResourceKind.UniformBuffer, _cameraBuffer);
        
        BuildPrimitivesBuffer(device);
        Register("planes", ResourceKind.StructuredBufferReadOnly, _primitiveBuffer);

        BufferDescription paramsBufferDesc = new()
        {
            SizeInBytes = 16,
            Usage = BufferUsage.UniformBuffer
        };
        _paramsBuffer = device.ResourceFactory.CreateBuffer(paramsBufferDesc);
        Register("params", ResourceKind.UniformBuffer, _paramsBuffer);
    }

    private void BuildPrimitivesBuffer(GraphicsDevice device)
    {
        BufferDescription primitiveBufferDesc = new()
        {
            SizeInBytes = _buffer.Stride * (uint)_buffer.Capacity,
            StructureByteStride = _buffer.Stride,
            Usage = BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic
        };
        _primitiveBuffer = device.ResourceFactory.CreateBuffer(primitiveBufferDesc);
    }

    private void RebuildPrimitivesBuffer(GraphicsDevice device)
    {
        _primitiveBuffer?.Dispose();
        BuildPrimitivesBuffer(device);
        UpdateBinding("planes", _primitiveBuffer!, device);
    }
    
    protected override void Upload(FrameContext ctx)
    {
        if (_bufferVersion != _buffer.CapacityVersion)
        {
            ctx.Device.WaitForIdle();
            RebuildPrimitivesBuffer(ctx.Device);
            _bufferVersion = _buffer.CapacityVersion;
            _buffer.MarkAllDirty();
            logger.LogInformation($"Primitives buffer was rebuilt to {_primitiveBuffer.SizeInBytes} bytes."); 
        }
        
        for (int i = 0; i < _buffer.Count; i++)
        {
            if (_buffer.IsDirty(i))
            {
                ctx.Cmd.UpdateBuffer(_primitiveBuffer, (uint)i * _buffer.Stride, _buffer.Data[i]);
            }
        }
        _buffer.ClearDirty();
        
        Params @params = new() { Count = (uint)_buffer.Count };
        ctx.Cmd.UpdateBuffer(_paramsBuffer, 0, @params);

        ctx.Cmd.UpdateBuffer(_cameraBuffer, 0, view.Data);
    }

    protected override void DisposeResources()
    {
        _sampler.Dispose();
        _primitiveBuffer.Dispose();
        _paramsBuffer.Dispose();
        _cameraBuffer.Dispose();
    }
}