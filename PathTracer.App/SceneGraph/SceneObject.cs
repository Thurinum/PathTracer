using PathTracerSceneGraph;

namespace PathTracerApp.SceneGraph;

public class SceneObject
{
    public required Scene Owner { get; init; }
    public List<Component> Components { get; } = [];

    public T AddComponent<T>() where T : Component
    {
        return Owner.ComponentFactory.CreateComponent<T>(this);
    }

    public T GetComponent<T>() where T : Component
    {
        return Components.OfType<T>().Single();
    }

    public bool TryGetComponent<T>(out T? component) where T : Component
    {
        component = Components.OfType<T>().SingleOrDefault();
        return component != null;
    }
    
    public T[] GetComponents<T>() where T : Component
    {
        return Components.OfType<T>().ToArray();
    }
}
