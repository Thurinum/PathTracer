namespace PathTracerCore.Renderer;

public readonly record struct Color(float R, float G, float B)
{
    public static readonly Color Red = new(1.0f, 0.0f, 0.0f);
}
