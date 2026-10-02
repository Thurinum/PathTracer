namespace PathTracerCore.Bvh;

public interface IBvhReady
{
    AxisAlignedBoundingBox BvhBounds { get; }
    uint BvhType { get; }
}