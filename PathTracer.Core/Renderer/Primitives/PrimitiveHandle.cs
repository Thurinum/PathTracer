namespace PathTracerCore.Renderer.Primitives;

public readonly record struct PrimitiveHandle(int Index, int Version)
{
    public static readonly PrimitiveHandle Invalid = new(-1, 0);
    public bool IsValid => Index >= 0;
}