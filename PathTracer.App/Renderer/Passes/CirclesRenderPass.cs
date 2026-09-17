using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Renderer.Pass;
using PathTracerApp.Shader;
using Silk.NET.Maths;

namespace PathTracerApp.Renderer.Passes;

public struct Params
{
    public uint Count;
}

public class CirclesRenderPass(SlangCompiler compiler, IOptions<RenderBackendOptions> options, RenderPrimitives<CirclePrimitive> primitives) : RenderPass(compiler, options)
{
    protected override string ShaderModule => "circles";
    
    private DeviceBuffer _primitiveBuffer = null!;
    private DeviceBuffer _paramsBuffer = null!;
    
    protected override void SetupResources(GraphicsDevice device)
    {
        BufferDescription primitiveBufferDesc = new()
        {
            SizeInBytes = primitives.SizeInBytes,
            StructureByteStride = primitives.Stride,
            Usage = BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic
        };
        _primitiveBuffer = device.ResourceFactory.CreateBuffer(primitiveBufferDesc);

        BufferDescription paramsBufferDesc = new()
        {
            SizeInBytes = 16,
            Usage = BufferUsage.UniformBuffer
        };
        _paramsBuffer = device.ResourceFactory.CreateBuffer(paramsBufferDesc);
        
        Register("circles", ResourceKind.StructuredBufferReadOnly, _primitiveBuffer);
        Register("params", ResourceKind.UniformBuffer, _paramsBuffer);
    }

    protected override void Upload(CommandList cmd)
    {
        Params @params = new() { Count = (uint)primitives.Count };

        for (int i = 0; i < primitives.Count; i++)
        {
            if (!primitives.IsDirty(i))
                continue;
            
            cmd.UpdateBuffer(_primitiveBuffer, (uint)i * primitives.Stride, primitives.State[i]);
            primitives.ClearDirty(i);
        }
        
        cmd.UpdateBuffer(_paramsBuffer, 0, @params);
    }

    protected override void DisposeResources()
    {
        _primitiveBuffer.Dispose();
        _paramsBuffer.Dispose();
    }
}