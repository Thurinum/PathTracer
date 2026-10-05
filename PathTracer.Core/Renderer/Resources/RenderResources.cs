using NeoVeldrid;

namespace PathTracerCore.Renderer.Resources;

public interface IResourceSpec { string Name { get; } }
public sealed record TextureSpec(string Name, PixelFormat Format, TextureUsage Usage, TextureSizePolicy SizePolicy) : IResourceSpec;
public sealed record SamplerSpec(string Name, SamplerDescription Description) : IResourceSpec;
public sealed record UniformBufferSpec(string Name, uint SizeInBytes) : IResourceSpec;
public sealed record StructuredBufferSpec(string Name, uint Stride, uint ElementCount) : IResourceSpec;
public interface IDynamicBufferSource
{
    uint ElementCount { get; }
    void Upload(CommandList cmd, DeviceBuffer buf, bool resized);
}
public sealed record DynamicBufferSpec(string Name, uint Stride, IDynamicBufferSource Source) : IResourceSpec;

public sealed record ResourceRef(string Name, ResourceKind Kind);