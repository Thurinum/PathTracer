namespace PathTracerCore.Renderer;

public interface IRenderer<T> where T : unmanaged
{
    T Bake();
}