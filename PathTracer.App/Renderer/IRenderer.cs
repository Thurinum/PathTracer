namespace PathTracerApp.Renderer;

public interface IRenderer<out T> where T : unmanaged
{
    T Bake();
}