using System.Numerics;
using System.Runtime.InteropServices;
using PathTracerCore.Renderer;
using PathTracerCore.SceneGraph;

namespace PathTracerApp.Components;

[StructLayout(LayoutKind.Sequential)]
public record struct SpherePrimitive
{
    public Vector3 Center;
    public float Radius;
    public Color Color;
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