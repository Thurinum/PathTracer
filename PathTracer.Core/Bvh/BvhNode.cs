namespace PathTracerCore.Bvh;

public readonly record struct BvhPrimitiveRef(
    uint Type,
    uint Index,
    AxisAlignedBoundingBox Bounds);
    
public sealed class BvhNode
{
    public required AxisAlignedBoundingBox Bounds;
    public BvhNode? Left;
    public BvhNode? Right;
    public List<BvhPrimitiveRef>? Primitives;
}