namespace PathTracerCore.Renderer.Passes;

public sealed class RenderGraphBuilder
{
    internal List<Type> PassTypes { get; } = [];
    internal List<SharedTextureDesc> SharedTextures { get; } = [];

    public void AddPass<T>() where T : RenderPass
    {
        if (typeof(T) == typeof(RenderPass))
            throw new InvalidOperationException("You must subclass RenderPass to create your own.");

        PassTypes.Add(typeof(T));
    }

    public void AddSharedTexture(SharedTextureDesc descriptor)
    {
        SharedTextures.Add(descriptor);
    }
}