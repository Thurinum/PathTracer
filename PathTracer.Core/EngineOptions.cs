namespace PathTracerCore;

public sealed class EngineOptions
{
    public string AppName = "Path Tracer";
    public uint WindowWidth;
    public uint WindowHeight;
    public int X;
    public int Y;
    
    public bool VSync = true;
    public string ShadersDir = "Shaders";
    
    // TODO: This should be reflected from the shader
    public (uint, uint, uint) ThreadGroupSize = (8, 8, 1);
}