namespace PathTracerCore.Renderer.Passes;

public sealed class RenderPassSelector
{
    internal Type? PassType { get; private set; }

    public void Use<T>() where T : RenderPass
    {
        if (typeof(T) == typeof(RenderPass))
            throw new InvalidOperationException("You must subclass RenderPass to create your own.");
        
        PassType = typeof(T);
    }
}