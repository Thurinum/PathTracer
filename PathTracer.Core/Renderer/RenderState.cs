using PathTracerCore.SceneGraph;

namespace PathTracerCore.Renderer;

public sealed class RenderState(
    RenderPrimitives<CirclePrimitive> circles)
{
    public void Collect(Scene scene)
    {
        foreach (var obj in scene.Objects)
        {
            foreach (var c in obj.Components)
            {
                if (c is IRenderer<CirclePrimitive> r)
                {
                    circles.Register(r);
                }
            }
        }
    }

    public void Bake()
    {
        circles.BakeRenderState();
    }
}