using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Components;
using PathTracerCore;
using PathTracerCore.Renderer;
using PathTracerCore.Renderer.Passes;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.Renderer.Shaders;

namespace PathTracerApp.RenderPasses;

public struct Params
{
    public uint Count;
}

public class CirclesRenderPass(ILogger<CirclesRenderPass> logger, SlangCompiler compiler, IOptions<EngineOptions> options, PrimitiveRegistry primitives) : RenderPass(compiler, options)
{
    protected override string ShaderModule => "circles";
    
    private DeviceBuffer _primitiveBuffer = null!;
    private DeviceBuffer _paramsBuffer = null!;
    private PrimitiveBuffer<CirclePrimitive> _buffer = null!;
    private int _bufferVersion = 0;
    
    protected override void SetupResources(GraphicsDevice device)
    {
        _buffer = primitives.Get<CirclePrimitive>();
        
        BuildPrimitivesBuffer(device);

        BufferDescription paramsBufferDesc = new()
        {
            SizeInBytes = 16,
            Usage = BufferUsage.UniformBuffer
        };
        _paramsBuffer = device.ResourceFactory.CreateBuffer(paramsBufferDesc);
        
        Register("circles", ResourceKind.StructuredBufferReadOnly, _primitiveBuffer);
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
        UpdateBinding("circles", _primitiveBuffer!, device);
    }
    
    protected override void Upload(FrameContext ctx)
    {
        Params @params = new() { Count = (uint)_buffer.Count };
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
        
        ctx.Cmd.UpdateBuffer(_paramsBuffer, 0, @params);
    }

    protected override void DisposeResources()
    {
        _primitiveBuffer.Dispose();
        _paramsBuffer.Dispose();
    }
}