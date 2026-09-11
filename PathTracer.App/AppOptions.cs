namespace PathTracerApp;

public sealed class AppOptions
{
    public string AppName = "Path Tracer";
    public uint WindowWidth;
    public uint WindowHeight;
    public (int, int, int) ThreadGroupSize = (8, 8, 1);
}