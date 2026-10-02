using System.Numerics;
using System.Runtime.InteropServices;
using PathTracerCore.Renderer;
using PathTracerCore.Bvh;
using PathTracerCore.SceneGraph;

namespace PathTracerApp.Components;

[StructLayout(LayoutKind.Sequential)]
public record struct SpherePrimitive : IBvhReady
{
    public Vector3 Center;
    public float Radius;
    public Color Color;
    
    public uint BvhType => 1;
    
    public AxisAlignedBoundingBox BvhBounds => new AxisAlignedBoundingBox
    {
        Min = Center - new Vector3(Radius),
        Max = Center + new Vector3(Radius)
    };
}

public class SphereComponent : RendererBase<SpherePrimitive>
{
    public float Radius { get; set; } = 1.0f;
    public Color Color { get; set; } = Color.Red;

    protected override SpherePrimitive BuildPrimitive()
    {
        return new SpherePrimitive
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            Color = Color
        };
    }
}