using NeoVeldrid;

namespace PathTracerCore.Renderer.Graph;

public interface IResourceSpec { string Name { get; } }
public sealed record TextureSpec(string Name, PixelFormat Format, TextureUsage Usage, TextureSizePolicy SizePolicy) : IResourceSpec;
public sealed record UniformBufferSpec(string Name, uint SizeInBytes) : IResourceSpec;
public sealed record StructuredBufferSpec(string Name, uint Stride, uint ElementCount) : IResourceSpec;
public interface IDynamicBufferSource
{
    uint ElementCount { get; }
    void Upload(CommandList cmd, DeviceBuffer buf);
}
public sealed record DynamicBufferSpec(string Name, uint Stride, IDynamicBufferSource Source) : IResourceSpec;

public sealed record ResourceRef(string Name, ResourceKind Kind);
public sealed record BindingSpec(string GPUName, string CPUName, ResourceKind Kind);
public sealed record SamplerSpec(string GPUName, SamplerDescription Description);

// Used by passes to access resources managed by the graph.
public interface IResourceAccessor
{
    Texture GetTexture(string name);
    DeviceBuffer GetBuffer(string name);

    Texture Read(string name);
    Texture Write(string name);
}

public interface IResourceProvider
{
    IEnumerable<IResourceSpec> Resources { get; }
    void Update(RenderContext ctx);
}