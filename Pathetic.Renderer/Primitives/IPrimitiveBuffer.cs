using Pathetic.Renderer.Resources;

namespace Pathetic.Renderer.Primitives;

public interface IPrimitiveBuffer : IDynamicBufferSource
{
    Type PrimitiveType { get; }
    int Capacity { get; }
    int Count { get; }
    uint Stride { get; }
}
