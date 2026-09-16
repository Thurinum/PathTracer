using Microsoft.Extensions.Logging;
using PathTracerApp.SceneGraph;

namespace PathTracerSceneGraph;

public abstract class Component
{
    public SceneObject Parent { get; internal set; } = null!;
    public ILogger Logger { get; internal set; } = null!;
    protected Scene Root => Parent.Owner;
    
    public virtual void Awake() {}
    public virtual void Start() {}
    public virtual void Update(float deltaTime) {}
    public virtual void LateUpdate(float deltaTime) {}
    public virtual void OnGUI() {}
    public virtual void OnDestroy() {}
}