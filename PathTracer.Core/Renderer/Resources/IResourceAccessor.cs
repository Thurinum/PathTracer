using NeoVeldrid;

namespace PathTracerCore.Renderer.Resources;

public interface IResourceAccessor
{
    BindableResource Get(string name);

    T Get<T>(string name) where T : class, BindableResource
    {
        return Get(name) as T ?? throw new NullReferenceException($"{name} is not a {typeof(T)}");
    } 
    
    Texture GetTexture(string name) => Get<Texture>(name);
    DeviceBuffer GetBuffer(string name) => Get<DeviceBuffer>(name);
}