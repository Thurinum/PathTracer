using System.Numerics;
using System.Runtime.InteropServices;
using PathTracerCore.SceneGraph;

namespace PathTracerApp.Components;

[StructLayout(LayoutKind.Explicit, Size = 32)]
public record struct PlanePrimitive
{
    [FieldOffset(00)] public Vector3 Point;
    [FieldOffset(16)] public Vector3 Normal;
}   

public class PlaneComponent : RendererBase<PlanePrimitive>
{
    public float Radius { get; set; }
    private Transform _transform = null!;

    public override void Awake()
    {
        _transform = Parent.GetComponent<Transform>();
        base.Awake();
    }

    protected override PlanePrimitive BuildPrimitive()
    {
        return new PlanePrimitive
        {
            Point = _transform.Position,
            Normal = Vector3.Transform(Vector3.UnitY, _transform.Rotation)
        };
    }
}