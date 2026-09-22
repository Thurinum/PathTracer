namespace PathTracerCore.Renderer.Passes;

public sealed class RenderPassSelector
{
    internal List<Type> PassTypes { get; } = [];

    public void Add<T>() where T : RenderPass
    {
        if (typeof(T) == typeof(RenderPass))
            throw new InvalidOperationException("You must subclass RenderPass to create your own.");

        PassTypes.Add(typeof(T));
    }
}