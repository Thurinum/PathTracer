using Pathetic.Renderer.Graph;

namespace Pathetic.Renderer.Resources;

public interface IResourceProvider
{
    IEnumerable<IResourceSpec> Resources { get; }
    void Update(RenderContext ctx);
}