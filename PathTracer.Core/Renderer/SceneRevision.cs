namespace PathTracerCore.Renderer;

public sealed class SceneRevision
{
    public uint Value { get; private set; }

    public void Invalidate() => Value++;
}
