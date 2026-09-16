using System.Numerics;
using System.Runtime.InteropServices;

namespace PathTracerApp.Renderer;

[StructLayout(LayoutKind.Sequential)]
public struct GPU_Primitive
{
    public Vector2 Position01;
    public float Radius;
    public float Pad;
}

public class RenderState
{
    private const int _capacity = 67;
    public GPU_Primitive[] State { get; private set; } = new GPU_Primitive[_capacity];
    private readonly bool[] _dirtyFlags = new bool[_capacity];
    private readonly List<IRenderer> _dirtyRenderers = [];
    
    public int Capacity => _capacity;
    public int Count { get; private set; } = 0;

    public int Register(IRenderer renderer)
    {
        int index = Count++;
        if (index >= _capacity)
            throw new InvalidOperationException("Renderer capacity exceeded. Shit renderer but blame the machine not me");
        
        _dirtyFlags[index] = true;
        _dirtyRenderers.Add(renderer);
        State[index] = default;
        
        return index;
    }
    
    public void MarkDirty(IRenderer renderer)
    {
        if (renderer.SlotIndex is not { } index)
            throw new InvalidOperationException("Renderer is not registered");

        // ensure we mark dirty once per frame
        if (_dirtyFlags[index])
            return;
        
        _dirtyFlags[index] = true;
        _dirtyRenderers.Add(renderer);
    }

    public void BakeRenderState()
    {
        foreach (var renderer in _dirtyRenderers)
        {
            if (renderer.SlotIndex is not { } index)
                throw new InvalidOperationException("Renderer is not registered");
            
            State[index] = renderer.Bake();
            _dirtyFlags[index] = false;
        }
        
        _dirtyRenderers.Clear();
    }
}