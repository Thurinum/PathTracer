using PathTracerCore.Renderer.Graph;
using PathTracerCore.Renderer.Resources;

namespace PathTracerCore.Renderer.Primitives;

public sealed class PrimitiveRegistry : IResourceProvider
{
    private readonly Dictionary<Type, IPrimitiveBuffer> _buffers = [];
    private readonly List<(string Name, IPrimitiveBuffer Buffer)> _entries = [];

    public IEnumerable<IPrimitiveBuffer> All => _buffers.Values;

    public void Register<T>(string name) where T : unmanaged
    {
        var buffer = new PrimitiveBuffer<T>();
        if (!_buffers.TryAdd(typeof(T), buffer))
        {
            throw new InvalidOperationException($"{typeof(T).Name} is already registered.");
        }

        _entries.Add((name, buffer));
    }

    public PrimitiveBuffer<T> Get<T>() where T : unmanaged
    {
        if (_buffers.TryGetValue(typeof(T), out var buffer))
        {
            return (PrimitiveBuffer<T>)buffer;
        }

        throw new InvalidOperationException($"{typeof(T).Name} is not registered.");
    }

    public IEnumerable<IResourceSpec> Resources =>
        _entries.Select(e => new DynamicBufferSpec(e.Name, e.Buffer.Stride, e.Buffer));

    public void Update(RenderContext ctx) { }
}
