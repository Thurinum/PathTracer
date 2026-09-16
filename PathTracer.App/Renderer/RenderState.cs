using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracerApp.Renderer;

[StructLayout(LayoutKind.Sequential)]
public record struct GPU_Primitive
{
    public Vector2 Position01;
    public float Radius;
    public float Pad;
}

// todo: need to change this it's pretty useless since we O(n) bake anyway just so we can delta upload to gpu
public class RenderState
{
    private const int _capacity = 1000;
    public GPU_Primitive[] State { get; } = new GPU_Primitive[_capacity];
    public bool[] DirtyBits { get; } = new bool[_capacity];
    private readonly List<IRenderer> _renderers = [];
    
    public int Capacity => _capacity;
    public int Count { get; private set; } = 0;

    public int Register(IRenderer renderer)
    {
        int index = Count++;
        if (index >= _capacity)
            throw new InvalidOperationException("Renderer capacity exceeded. Shit renderer but blame the machine not me");
        
        DirtyBits[index] = true;
        _renderers.Add(renderer);
        State[index] = default;
        
        return index;
    }

    public void BakeRenderState()
    {
        foreach (var renderer in _renderers)
        {
            if (renderer.SlotIndex is not { } index)
                throw new InvalidOperationException("Renderer is not registered");
            
            var primitive = renderer.Bake();
            if (!primitive.Equals(State[index]))
            {
                DirtyBits[index] = true;
                State[index] = primitive;
            }
        }
    }
}