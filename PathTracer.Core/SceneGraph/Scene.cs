namespace PathTracerCore.SceneGraph;

public class Scene(ComponentFactory componentFactory)
{
    internal ComponentFactory ComponentFactory { get; } = componentFactory;
    public List<SceneObject> Objects { get; } = [];
}
