namespace PathTracerCore.Renderer.Primitives;

public interface IPrimitiveBuffer
{
    Type PrimitiveType { get; }
    int Capacity { get; }
    int Count { get; }
    uint Stride { get; }
}