using Microsoft.Extensions.Options;
using NeoVeldrid;
using PathTracerApp.Renderer.Pass;
using PathTracerApp.Shader;

namespace PathTracerApp.Renderer.Passes;

public struct Params
{
    public uint Count;
}

public class CirclesRenderPass(SlangCompiler compiler, IOptions<RenderBackendOptions> options, RenderState state) : RenderPass(compiler, options)
{
    protected override string ShaderModule => "circles";
    
    private DeviceBuffer _primitiveBuffer = null!;
    private DeviceBuffer _paramsBuffer = null!;
    
    protected override void SetupResources(GraphicsDevice device)
    {
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
        
        Register("circles", ResourceKind.StructuredBufferReadOnly, _primitiveBuffer);
        Register("params", ResourceKind.UniformBuffer, _paramsBuffer);
    }

    protected override void Upload(CommandList cmd)
    {
        Params @params = new() { Count = (uint)state.Count };

        for (int i = 0; i < state.Count; i++)
        {
            if (!state.DirtyBits[i])
                continue;
            
            cmd.UpdateBuffer(_primitiveBuffer, (uint)i * 16, state.State[i]);
            state.DirtyBits[i] = false;
        }
        
        cmd.UpdateBuffer(_paramsBuffer, 0, @params);
    }

    protected override void DisposeResources()
    {
        _primitiveBuffer.Dispose();
        _paramsBuffer.Dispose();
    }
}