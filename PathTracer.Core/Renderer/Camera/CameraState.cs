namespace PathTracerCore.Renderer.Camera;

public sealed class CameraState
{
    public CameraData Data;
    public uint Version { get; private set; }

    public void Update(in CameraData data)
    {
        if (data.Equals(Data))
            return;
        
        Data = data;
        Version++;
    }
}