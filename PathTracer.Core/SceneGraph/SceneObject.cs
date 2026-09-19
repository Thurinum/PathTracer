namespace PathTracerCore.SceneGraph;

public class SceneObject
{
    public required string Name { get; init; }
    public required Scene Owner { get; init; }
    public List<Component> Components { get; } = [];

    public T AddComponent<T>() where T : Component
    {
        return Owner.ComponentFactory.CreateComponent<T>(this);
    }

    public T GetComponent<T>() where T : Component
    {
        var components = GetComponents<T>();
        
        if (components.Length == 0)
            throw new Exception($"No component of type {typeof(T)} found on {this}");
        
        if (components.Length > 1)
            throw new Exception($"Multiple components of type {typeof(T)} found on {this}");

        return components[0];
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
