namespace PathTracerCore.Renderer.Graph;

public sealed class RenderGraphBuilder
{
    internal List<Type> PassTypes { get; } = [];
    internal string? PresentTextureName;

    public void AddPass<T>() where T : RenderPass
    {
        if (typeof(T) == typeof(RenderPass))
            throw new InvalidOperationException("You must subclass RenderPass to create your own.");

        PassTypes.Add(typeof(T));
    }

    public void Present(string textureName)
    {
        PresentTextureName = textureName;
    }
}