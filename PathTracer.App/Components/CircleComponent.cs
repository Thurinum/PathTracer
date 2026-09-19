using System.Numerics;
using System.Runtime.InteropServices;
using PathTracerCore.Renderer;
using PathTracerCore.SceneGraph;

namespace PathTracerApp.Components;

[StructLayout(LayoutKind.Sequential)]
public record struct CirclePrimitive
{
    public Vector2 Position01;
    public float Radius;
    public float Pad;
}

public class CircleComponent : RendererBase<CirclePrimitive>
{
    public float Radius { get; set; }
    private Transform _transform = null!;

    public override void Awake()
    {
        _transform = Parent.GetComponent<Transform>();
        base.Awake();
    }

    protected override CirclePrimitive BuildPrimitive()
    {
        return new CirclePrimitive
        {
            Position01 = _transform.Position.XY,
            Radius = Radius
        };
    }
}