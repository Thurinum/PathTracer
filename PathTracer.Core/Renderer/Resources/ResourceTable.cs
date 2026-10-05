using System.Diagnostics;
using NeoVeldrid;
using PathTracerCore.Utils;

namespace PathTracerCore.Renderer.Resources;

internal sealed class ResourceTable : IResourceAccessor, IDisposable
{
    private readonly Dictionary<string, RenderGraphResource> _resources = [];

    public void Declare(IResourceSpec spec)
    {
        var resource = new RenderGraphResource
        {
            Spec = spec
        };

        if (!_resources.TryAdd(spec.Name, resource))
        {
            throw new ArgumentException($"Resource {spec.Name} already declared.");
        }
    }

    private const int DynamicBufferDefaultElementCount = 256;
    public void Allocate(GraphicsDevice device, uint width, uint height)
    {
        foreach (var entry in _resources.Values)
        {
            entry.Resource = entry.Spec switch
            {
                UniformBufferSpec s => CreateUniformBuffer(device, s),
                TextureSpec s => CreateTexture(device, s, width, height),
                SamplerSpec s => CreateSampler(device, s),
                StructuredBufferSpec s => CreateStructuredBuffer(device, s),
                DynamicBufferSpec s => CreateDynamicBuffer(device, s, entry, Math.Max(DynamicBufferDefaultElementCount, s.Source.ElementCount)),
                _ => throw new UnreachableException("bruh")
            };
        }
    }
    
    public IReadOnlyList<RenderGraphResource> Refresh(GraphicsDevice device, CommandList cmd)
    {
        List<RenderGraphResource> reallocated = []; // TODO: Wasteful frequent List allocation?
        
        foreach (RenderGraphResource entry in _resources.Values)
        {
            if (entry.Spec is not DynamicBufferSpec spec)
                continue;

            bool didReallocate = false;
            uint requiredCapacity = spec.Source.ElementCount;
            if (requiredCapacity > entry.ElementCapacity)
            {
                if (reallocated.Count == 0)
                {
                    device.WaitForIdle();
                }
                
                reallocated.Add(entry);
                uint newCapacity = Math.Max(spec.Source.ElementCount, entry.ElementCapacity * 2);
                entry.Resource = CreateDynamicBuffer(device, spec, entry, newCapacity);
                entry.Version++;
                didReallocate = true;
            }
            
            spec.Source.Upload(cmd, (DeviceBuffer)entry.Resource!, didReallocate);
        }

        return reallocated;
    }

    public void Resize(GraphicsDevice device, uint width, uint height)
    {
        device.WaitForIdle();
        
        foreach (RenderGraphResource entry in _resources.Values)
        {
            if (entry.Spec is TextureSpec { SizePolicy: AutoSize } spec)
            {
                entry.Resource = CreateTexture(device, spec, width, height);
                entry.Version++;
            }
        }
    }
    
    public BindableResource Get(string name)
    {
        if (!_resources.TryGetValue(name, out RenderGraphResource? value))
            throw new ArgumentException($"No resource named {name} registered.");
        
        return value.Resource ?? throw new NullReferenceException($"Accessed resource at {name} but it is null.");
    }

    public T Get<T>(string name) where T : class, BindableResource
    {
        return Get(name) as T ?? throw new NullReferenceException($"{name} is not a {typeof(T)}");
    } 
    
    public Texture GetTexture(string name) => Get<Texture>(name);
    public DeviceBuffer GetBuffer(string name) => Get<DeviceBuffer>(name);

    public bool Has(string name)
    {
        return Has<BindableResource>(name);
    }
    
    public bool Has<T>(string name) where T : BindableResource
    {
        return _resources.TryGetValue(name, out var resource) && resource.Resource is T;
    }
    
    public void Dispose()
    {
        foreach (var resource in _resources.Values)
        {
            resource.Owner?.Dispose();
        }
    }

    private static DeviceBuffer CreateUniformBuffer(GraphicsDevice device, UniformBufferSpec spec)
    {
        var bufferDesc = new BufferDescription(spec.SizeInBytes, BufferUsage.UniformBuffer);
        return device.ResourceFactory.CreateBuffer(bufferDesc);
    }
    
    private static Texture CreateTexture(GraphicsDevice device, TextureSpec spec, uint framebufferWidth, uint framebufferHeight)
    {
        uint2 size = spec.SizePolicy switch
        {
            ExplicitSize s => (s.Width, s.Height),
            AutoSize       => (framebufferWidth, framebufferHeight),
            _ => throw new NotSupportedException()
        };

        return device.ResourceFactory.CreateTexture(new TextureDescription
        {
            Width = size.x, 
            Height = size.y, 
            Depth = 1,
            MipLevels = 1, 
            ArrayLayers = 1,
            Format = spec.Format, 
            Usage = spec.Usage,
            Type = TextureType.Texture2D, 
            SampleCount = TextureSampleCount.Count1
        });
    }
    
    private static Sampler CreateSampler(GraphicsDevice device, SamplerSpec samplerSpec)
    {
        return device.ResourceFactory.CreateSampler(samplerSpec.Description);
    }
    
    private static DeviceBuffer CreateStructuredBuffer(GraphicsDevice device, StructuredBufferSpec spec)
    {
        var bufferDesc = new BufferDescription(spec.Stride * spec.ElementCount, BufferUsage.StructuredBufferReadOnly, spec.Stride);
        return device.ResourceFactory.CreateBuffer(bufferDesc);
    }
    
    private static DeviceBuffer CreateDynamicBuffer(GraphicsDevice device, DynamicBufferSpec spec, RenderGraphResource entry, uint elementCapacity = 256)
    {
        if (elementCapacity <= 0)
            throw new ArgumentException("Element capacity must be greater than 0.");
        
        entry.ElementCapacity = elementCapacity;
        var bufferDesc = new BufferDescription(spec.Stride * entry.ElementCapacity, BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, spec.Stride);
        return device.ResourceFactory.CreateBuffer(bufferDesc);
    }
}