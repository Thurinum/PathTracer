using NeoVeldrid;

namespace Pathetic.Renderer.Resources;

public sealed class RenderGraphResource
{
    public required IResourceSpec Spec;

    public BindableResource? Resource
    {
        get;
        set
        {
            Owner?.Dispose();
            field = value;
            Owner = (IDisposable)value!;
        }
    }

    public IDisposable? Owner { get; private set; }
    public uint ElementCapacity;
    public uint Version;
}