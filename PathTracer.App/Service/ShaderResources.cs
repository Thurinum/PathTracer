using NeoVeldrid;

namespace PathTracerApp.Shader;

public sealed class ShaderResources
{
    private readonly GraphicsDevice _device;
    private readonly List<ResourceLayoutElementDescription> _elements = new();

    public ShaderResources(GraphicsDevice device)
    {
        _device = device;
    }

    public ShaderResources Add(string name, ResourceKind kind, ShaderStages stages)
    {
        _elements.Add(new ResourceLayoutElementDescription(name, kind, stages));
        return this;
    }

    public ResourceLayout CreateLayout()
    {
        return _device.ResourceFactory.CreateResourceLayout(
            new ResourceLayoutDescription(_elements.ToArray()));
    }

    public ResourceSet CreateResourceSet(ResourceLayout layout, params BindableResource[] resources)
    {
        return _device.ResourceFactory.CreateResourceSet(
            new ResourceSetDescription(layout, resources));
    }

    public static void Bind(CommandList commands, uint slot, ResourceSet resourceSet, ShaderStages stages)
    {
        if ((stages & ShaderStages.Compute) != 0)
        {
            commands.SetComputeResourceSet(slot, resourceSet);
        }
        else
        {
            commands.SetGraphicsResourceSet(slot, resourceSet);
        }
    }
}
