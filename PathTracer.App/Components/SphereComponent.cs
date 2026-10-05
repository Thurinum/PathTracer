using System.Numerics;
using System.Runtime.InteropServices;
using PathTracerCore.Renderer;
using PathTracerCore.SceneGraph;
using PathTracerCore.Utils;

namespace PathTracerApp.Components;

[StructLayout(LayoutKind.Explicit, Size = 48)]
public record struct SpherePrimitive
{
    [FieldOffset(00)] public Vector3 Center;
    [FieldOffset(12)] public float Radius;
    [FieldOffset(16)] public Color Albedo;
    [FieldOffset(32)] public Color Emission;
}

public class SphereComponent : RendererBase<SpherePrimitive>
{
    public float Radius { get; set; } = 1.0f;
    public Color Albedo { get; set; } = new(0.5f, 0.5f, 0.5f);
    public Color Emission { get; set; }

    protected override SpherePrimitive BuildPrimitive()
    {
        return new SpherePrimitive
        {
            Center = Parent.Transform.Position,
            Radius = Radius,
            Albedo = Albedo,
            Emission = Emission
        };
    }
}