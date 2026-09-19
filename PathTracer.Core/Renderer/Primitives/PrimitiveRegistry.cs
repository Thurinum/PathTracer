namespace PathTracerCore.Renderer.Primitives;

public sealed class PrimitiveRegistry
{
    private readonly Dictionary<Type, IPrimitiveBuffer> _primTypeToBuffer = [];

    public IEnumerable<IPrimitiveBuffer> All => _primTypeToBuffer.Values;
    public PrimitiveBuffer<T> Get<T>() where T : unmanaged
    {
        if (_primTypeToBuffer.TryGetValue(typeof(T), out var buffer))
        {
            return (PrimitiveBuffer<T>)buffer;
        }

        return (PrimitiveBuffer<T>)(_primTypeToBuffer[typeof(T)] = new PrimitiveBuffer<T>());
    }
}