using PathTracerSceneGraph;

namespace PathTracerApp.Renderer;

public class CircleRenderer(RenderState state) : Component, IRenderer
{
    public int? SlotIndex { get; private set; }
    public float Radius { get; set; }

    public override void Awake()
    {
        SlotIndex = state.Register(this);
    }

    public GPU_Primitive Bake()
    {
        // todo: requirecomponents
        var transform = Parent.GetComponent<Transform>();

        return new GPU_Primitive
        {
            Position01 = transform.Position.XY,
            Radius = Radius
        };
    }
}