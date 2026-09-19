using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PathTracerCore.Renderer.Primitives;

namespace PathTracerCore.SceneGraph;

public class ComponentFactory(IServiceProvider provider, ILoggerFactory loggerFactory, PrimitiveRegistry primitives)
{
    public T CreateComponent<T>(SceneObject parent) where T : Component
    {
        var component = ActivatorUtilities.CreateInstance<T>(provider);
        component.Parent = parent;
        component.Logger = loggerFactory.CreateLogger<T>();
        component.Primitives = primitives;
        parent.Components.Add(component);
        return component;
    }
}
