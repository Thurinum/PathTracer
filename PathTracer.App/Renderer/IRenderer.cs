namespace PathTracerApp.Renderer;

public interface IRenderer
{
    int? SlotIndex { get; }
    GPU_Primitive Bake();
}