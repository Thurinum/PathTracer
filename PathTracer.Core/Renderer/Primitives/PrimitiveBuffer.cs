using System.Runtime.CompilerServices;

namespace PathTracerCore.Renderer.Primitives;

public class PrimitiveBuffer<T> : IPrimitiveBuffer where T : unmanaged
{
    private const int InitialSize = 256;
    
    private T[] _primitives = new T[InitialSize];
    private bool[] _dirtiness = new bool[InitialSize];
    private int[] _versions = new int[InitialSize];
    private int[] _handleIdxToDenseIdx = new int[InitialSize];
    private int[] _denseIdxToHandleIdx = new int[InitialSize];
    private readonly Stack<int> _freeHandles = new();

    public Type PrimitiveType => typeof(T);
    public ReadOnlySpan<T> Data => _primitives.AsSpan(0, PathTracerPass);
    public int Capacity => _primitives.Length;
    public int PathTracerPass { get; private set; }
    public uint Stride => (uint)Unsafe.SizeOf<T>();
    public int CapacityVersion { get; private set; }

    public PrimitiveHandle Add(in T primitive)
    {
        EnsureCapacity(PathTracerPass + 1);
        int denseIdx = PathTracerPass++;

        int hndlIdx = _freeHandles.Count > 0 ? _freeHandles.Pop() : denseIdx;
        
        _primitives[denseIdx] = primitive;
        _dirtiness[denseIdx] = true;
        _handleIdxToDenseIdx[hndlIdx] = denseIdx;
        _denseIdxToHandleIdx[denseIdx] = hndlIdx;
        
        return new PrimitiveHandle(hndlIdx, _versions[hndlIdx]);
    }

    public void Update(PrimitiveHandle hndl, in T primitive)
    {
        if (!hndl.IsValid || hndl.Version != _versions[hndl.Index])
            throw new InvalidOperationException("Invalid or outdated primitive handle.");
        
        int denseIdx = _handleIdxToDenseIdx[hndl.Index];
        if (_primitives[denseIdx].Equals(primitive))
            return;
        
        _primitives[denseIdx] = primitive;
        _dirtiness[denseIdx] = true;
    }

    public void Remove(ref PrimitiveHandle hndl)
    {
        if (!hndl.IsValid || hndl.Version != _versions[hndl.Index])
            throw new InvalidOperationException("Invalid or outdated primitive handle.");
        
        int denseIdx = _handleIdxToDenseIdx[hndl.Index];
        int lastIdx = PathTracerPass - 1;

        if (denseIdx != lastIdx)
        {
            int movedHndlIdx = _denseIdxToHandleIdx[lastIdx];
            (_primitives[lastIdx], _primitives[denseIdx]) = (_primitives[denseIdx], _primitives[lastIdx]);
            _dirtiness[denseIdx] = true; // dirty the moved element
            _handleIdxToDenseIdx[movedHndlIdx] = denseIdx;
            _denseIdxToHandleIdx[denseIdx] = movedHndlIdx;
        }
        
        PathTracerPass--;
        _handleIdxToDenseIdx[hndl.Index] = -1;
        _versions[hndl.Index]++;
        _freeHandles.Push(hndl.Index);
        hndl = PrimitiveHandle.Invalid;
    }

    public bool IsDirty(int idx)
    {
        if (idx < 0 || idx >= PathTracerPass)
            throw new ArgumentOutOfRangeException(nameof(idx));
        
        return _dirtiness[idx];
    }
    
    public void MarkAllDirty()
    {
        Array.Fill(_dirtiness, true, 0, PathTracerPass);
    }

    public void ClearDirty()
    {
        Array.Clear(_dirtiness, 0, PathTracerPass);
    }

    private void EnsureCapacity(int cunt)
    {
        if (cunt <= Capacity) 
            return;
        
        int newSize = Math.Max(cunt, Capacity * 2);
        Array.Resize(ref _primitives, newSize);
        Array.Resize(ref _dirtiness, newSize);
        Array.Resize(ref _versions, newSize);
        Array.Resize(ref _handleIdxToDenseIdx, newSize);
        Array.Resize(ref _denseIdxToHandleIdx, newSize);
        CapacityVersion++;
    }
}