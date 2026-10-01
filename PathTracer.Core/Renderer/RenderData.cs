namespace PathTracerCore.Renderer;

public sealed class RenderData<T> where T : unmanaged
{
    public T Data;
    public uint Version { get; private set; }

    public void Update(in T data)
    {
        if (data.Equals(Data))
            return;
        
        Data = data;
        Version++;
    }
}