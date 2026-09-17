using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PathTracerApp.Renderer;

[StructLayout(LayoutKind.Sequential)]
public record struct CirclePrimitive
{
    public Vector2 Position01;
    public float Radius;
    public float Pad;
}

public sealed class RenderPrimitives<T> where T : unmanaged
{
    private const int DefaultSize = 1000;
    private readonly T[] _primitives = new T[DefaultSize];
    private readonly bool[] _dirtiness = new bool[DefaultSize];
    private readonly List<IRenderer<T>> _renderers = [];
    
    public uint Stride = (uint)Unsafe.SizeOf<T>();
    public uint SizeInBytes => (uint)Capacity * Stride;
    public int Capacity => _primitives.Length;
    public int Count { get; private set; } = 0;
    public ReadOnlySpan<T> State => _primitives.AsSpan(0, Count);
    public bool IsDirty(int i) => _dirtiness[i];
    public void ClearDirty(int i) => _dirtiness[i] = false;
    
    public int Register(IRenderer<T> renderer)
    {
        int index = Count++;
        if (index >= Capacity)
            throw new InvalidOperationException("No more space in the primitive buffer.");
        
        _dirtiness[index] = true;
        _renderers.Add(renderer);
        _primitives[index] = default;
        
        return index;
    }

    public void BakeRenderState()
    {
        for (int i = 0; i < _renderers.Count; i++)
        {
            IRenderer<T> renderer = _renderers[i];
            T primitive = renderer.Bake();
            if (primitive.Equals(State[i]))
                continue;

            _dirtiness[i] = true;
            _primitives[i] = primitive;
        }
    }
}