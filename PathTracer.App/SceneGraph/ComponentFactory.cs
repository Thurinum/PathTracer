using Microsoft.Extensions.DependencyInjection;
using PathTracerApp.SceneGraph;

namespace PathTracerSceneGraph;

public class ComponentFactory(IServiceProvider provider)
{
    public T CreateComponent<T>(SceneObject parent) where T : Component
    {
        T component = ActivatorUtilities.CreateInstance<T>(provider);
        component.Parent = parent;
        parent.Components.Add(component);
        return component;
    }
}
