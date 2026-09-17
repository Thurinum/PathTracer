using PathTracerSceneGraph;

namespace PathTracerApp.Renderer;

public class CircleRenderer : Component, IRenderer<CirclePrimitive>
{
    public float Radius { get; set; }
    private Transform _transform = null!;

    public override void Awake()
    {
        _transform = Parent.GetComponent<Transform>();
    }

    public CirclePrimitive Bake()
    {
        return new CirclePrimitive
        {
            Position01 = _transform.Position.XY,
            Radius = Radius
        };
    }
}