namespace PathTracerApp;

public sealed class RenderBackendOptions
{
    public string AppName = "Path Tracer";
    public uint WindowWidth;
    public uint WindowHeight;
    public int X;
    public int Y;
    public (uint, uint, uint) ThreadGroupSize = (8, 8, 1);
}