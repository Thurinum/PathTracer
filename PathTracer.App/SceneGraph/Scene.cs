using PathTracerApp.SceneGraph;

namespace PathTracerSceneGraph;

public class Scene(ComponentFactory componentFactory)
{
    internal ComponentFactory ComponentFactory { get; } = componentFactory;
    public List<SceneObject> Objects { get; } = [];
}
