using PathTracerCore.Renderer.Primitives;

namespace PathTracerCore.SceneGraph;

public abstract class RendererBase<T> : Component where T : unmanaged
{
    private PrimitiveBuffer<T> _primitives = null!;
    private PrimitiveHandle _handle = PrimitiveHandle.Invalid;

    public override void Awake()
    {
        _primitives = Primitives.Get<T>();
        _handle = _primitives.Add(BuildPrimitive());
    }

    public override void LateUpdate(float deltaTime)
    {
        if (_handle.IsValid)
        {
            _primitives.Update(_handle, BuildPrimitive());
        }
    }

    public override void OnDestroy()
    {
        if (_handle.IsValid)
        {
            _primitives.Remove(ref _handle);
        }
    }

    protected abstract T BuildPrimitive();
}