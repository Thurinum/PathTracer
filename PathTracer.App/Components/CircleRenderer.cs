using PathTracerSceneGraph;

namespace PathTracerApp.Renderer;

public class CircleRenderer(RenderState state) : Component, IRenderer
{
    public int? SlotIndex { get; private set; }
    public float Radius { get; set; }

    private Transform _transform = null!;

    public override void Awake()
    {
        SlotIndex = state.Register(this);
        _transform = Parent.GetComponent<Transform>();
    }

    public GPU_Primitive Bake()
    {
        return new GPU_Primitive
        {
            Position01 = _transform.Position.XY,
            Radius = Radius
        };
    }
}