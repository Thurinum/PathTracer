using Microsoft.Extensions.Logging;
using PathTracerCore.Renderer.Primitives;

namespace PathTracerCore.SceneGraph;

public abstract class Component : IDestroyable
{
    public SceneObject Parent { get; internal set; } = null!;
    public ILogger Logger { get; internal set; } = null!;
    protected Scene Root => Parent.Owner;
    public bool IsPendingDestroy { get; internal set; }
    public bool IsDestroyed { get; private set; }
    
    // set by the factory so child renderers don't need to inject by constructor
    internal PrimitiveRegistry Primitives = null!;
    
    public virtual void Awake() {}
    public virtual void Start() {}
    public virtual void Update(float deltaTime) {}
    public virtual void LateUpdate(float deltaTime) {}
    public virtual void OnGUI() {}
    public virtual void OnDestroy() {}

    public void Destroy()
    {
        if (IsDestroyed)
            return;

        IsDestroyed = true;
        OnDestroy();
        Parent.Components.Remove(this);
    }
}