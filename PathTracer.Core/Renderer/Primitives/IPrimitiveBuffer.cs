namespace PathTracerCore.Renderer.Primitives;

public interface IPrimitiveBuffer
{
    Type PrimitiveType { get; }
    int Capacity { get; }
    int PathTracerPass { get; }
    uint Stride { get; }
}