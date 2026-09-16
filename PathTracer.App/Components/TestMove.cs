using PathTracerSceneGraph;

namespace PathTracerApp.Renderer;

public class TestMove(RenderState state) : Component
{
    public float Speed { get; set; } = 1.0f;
    
    public override void Update(float deltaTime)
    {
        var circle = Parent.GetComponent<Transform>();
        circle.Position.X += Speed * deltaTime;
        
        state.MarkDirty(Parent.GetComponent<CircleRenderer>());
    }
}