using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PathTracerCore.Renderer.Primitives;
using PathTracerCore.SceneGraph;

namespace PathTracer.Core.Tests;

public class SceneLifecycleTests
{
    private static (SceneManager Manager, Scene Scene) NewScene()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var factory = new ComponentFactory(provider, NullLoggerFactory.Instance, new PrimitiveRegistry());
        var manager = new SceneManager(factory);
        return (manager, manager.CreateScene());
    }

    // ---------- scene / object creation ----------

    [Fact]
    public void CreateObject_AddsObjectToScene()
    {
        var (manager, scene) = NewScene();

        var obj = manager.CreateObject(scene, "root");

        Assert.Single(scene.Objects);
        Assert.Same(obj, scene.Objects[0]);
        Assert.Equal("root", obj.Name);
        Assert.Same(scene, obj.Owner);
        Assert.False(obj.IsPendingDestroy);
    }

    // ---------- AddComponent ----------

    [Fact]
    public void AddComponent_AttachesComponentAndWiresContext()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");

        var component = obj.AddComponent<ProbeComponent>();

        Assert.Contains(component, obj.Components);
        Assert.Same(obj, component.Parent);
        Assert.NotNull(component.Logger);
    }

    [Fact]
    public void AddComponent_DoesNotRunAwake()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");

        var component = obj.AddComponent<ProbeComponent>();

        Assert.Equal(0, component.AwakeCount); // the loop owns Awake
    }

    [Fact]
    public void AddComponent_OnPendingDestroyObject_Throws()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        manager.DestroyObject(scene, obj);

        Assert.Throws<InvalidOperationException>(() => { obj.AddComponent<ProbeComponent>(); });
    }

    // ---------- queries ----------

    [Fact]
    public void GetComponent_ReturnsAttachedComponent()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        var component = obj.AddComponent<ProbeComponent>();

        Assert.Same(component, obj.GetComponent<ProbeComponent>());
    }

    [Fact]
    public void GetComponent_WhenMissing_Throws()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");

        Assert.Throws<InvalidOperationException>(() => { obj.GetComponent<ProbeComponent>(); });
    }

    [Fact]
    public void GetComponent_WhenMultiple_Throws()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        obj.AddComponent<ProbeComponent>();
        obj.AddComponent<ProbeComponent>();

        Assert.Throws<InvalidOperationException>(() => { obj.GetComponent<ProbeComponent>(); });
    }

    [Fact]
    public void TryGetComponent_ReturnsFalseWhenMissing_TrueWhenPresent()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");

        Assert.False(obj.TryGetComponent<ProbeComponent>(out _));

        var component = obj.AddComponent<ProbeComponent>();

        Assert.True(obj.TryGetComponent<ProbeComponent>(out var found));
        Assert.Same(component, found);
    }

    [Fact]
    public void GetComponents_ReturnsAllOfRequestedType()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        obj.AddComponent<ProbeComponent>();
        obj.AddComponent<ProbeComponent>();
        obj.AddComponent<OtherComponent>();

        Assert.Equal(2, obj.GetComponents<ProbeComponent>().Length);
        Assert.Single(obj.GetComponents<OtherComponent>());
    }

    // ---------- RemoveComponent ----------

    [Fact]
    public void RemoveComponent_DefersUntilFlush()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        var component = obj.AddComponent<ProbeComponent>();

        obj.RemoveComponent<ProbeComponent>();

        Assert.Contains(component, obj.Components); // still alive this frame
        Assert.True(component.IsPendingDestroy);
        Assert.Equal(0, component.DestroyCount);

        scene.FlushDestroyed();

        Assert.DoesNotContain(component, obj.Components);
        Assert.Equal(1, component.DestroyCount);
        Assert.True(component.IsDestroyed);
    }

    [Fact]
    public void RemoveComponent_OnlyRemovesRequestedType()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        var probe = obj.AddComponent<ProbeComponent>();
        var other = obj.AddComponent<OtherComponent>();

        obj.RemoveComponent<ProbeComponent>();
        scene.FlushDestroyed();

        Assert.DoesNotContain(probe, obj.Components);
        Assert.Contains(other, obj.Components);
    }

    [Fact]
    public void RemoveComponent_NotAdded_Throws()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");

        Assert.Throws<InvalidOperationException>(() => obj.RemoveComponent<ProbeComponent>());
    }

    [Fact]
    public void RemoveComponent_Twice_InvokesOnDestroyOnce()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        var component = obj.AddComponent<ProbeComponent>();

        obj.RemoveComponent<ProbeComponent>();
        obj.RemoveComponent<ProbeComponent>();
        scene.FlushDestroyed();

        Assert.Equal(1, component.DestroyCount);
    }

    [Fact]
    public void RemoveComponent_OnPendingDestroyObject_Throws()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        obj.AddComponent<ProbeComponent>();
        manager.DestroyObject(scene, obj);

        Assert.Throws<InvalidOperationException>(() => obj.RemoveComponent<ProbeComponent>());
    }

    // ---------- DestroyObject ----------

    [Fact]
    public void DestroyObject_DefersUntilFlush()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        var component = obj.AddComponent<ProbeComponent>();

        manager.DestroyObject(scene, obj);

        Assert.Single(scene.Objects);
        Assert.True(obj.IsPendingDestroy);
        Assert.Equal(0, component.DestroyCount);

        scene.FlushDestroyed();

        Assert.Empty(scene.Objects);
        Assert.True(obj.IsDestroyed);
        Assert.Empty(obj.Components);
        Assert.Equal(1, component.DestroyCount);
    }

    [Fact]
    public void DestroyObject_Twice_InvokesOnDestroyOnce()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        var component = obj.AddComponent<ProbeComponent>();

        manager.DestroyObject(scene, obj);
        manager.DestroyObject(scene, obj);
        scene.FlushDestroyed();

        Assert.Equal(1, component.DestroyCount);
        Assert.Empty(scene.Objects);
    }

    [Fact]
    public void DestroyObject_WithIndividuallyQueuedComponent_InvokesOnDestroyOnce()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        var component = obj.AddComponent<ProbeComponent>();

        obj.RemoveComponent<ProbeComponent>(); // component queued first
        manager.DestroyObject(scene, obj);     // then the object

        scene.FlushDestroyed();

        Assert.Equal(1, component.DestroyCount);
        Assert.Empty(scene.Objects);
    }

    [Fact]
    public void DestroyObject_InvokesOnDestroyForEveryComponent()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");
        var probe = obj.AddComponent<ProbeComponent>();
        var other = obj.AddComponent<OtherComponent>();

        manager.DestroyObject(scene, obj);
        scene.FlushDestroyed();

        Assert.Equal(1, probe.DestroyCount);
        Assert.True(other.IsDestroyed);
    }

    // ---------- flush ----------

    [Fact]
    public void Flush_WithNothingPending_DoesNothing()
    {
        var (manager, scene) = NewScene();
        var obj = manager.CreateObject(scene, "o");

        scene.FlushDestroyed();

        Assert.Single(scene.Objects);
        Assert.False(obj.IsDestroyed);
    }

    [Fact]
    public void Flush_WhenOnDestroyDestroysAnotherObject_DefersToNextFlush()
    {
        var (manager, scene) = NewScene();
        var killer = manager.CreateObject(scene, "killer");
        var victim = manager.CreateObject(scene, "victim");
        var component = killer.AddComponent<DestroyOnDestroyComponent>();
        component.Manager = manager;
        component.Target = victim;

        manager.DestroyObject(scene, killer);
        scene.FlushDestroyed();

        // killer is gone; victim was queued during OnDestroy and is still alive
        Assert.Single(scene.Objects);
        Assert.Same(victim, scene.Objects[0]);

        scene.FlushDestroyed();

        Assert.Empty(scene.Objects);
    }

    // ---------- test doubles ----------

    public sealed class ProbeComponent : Component
    {
        public int AwakeCount { get; private set; }
        public int StartCount { get; private set; }
        public int UpdateCount { get; private set; }
        public int LateUpdateCount { get; private set; }
        public int GuiCount { get; private set; }
        public int DestroyCount { get; private set; }

        public override void Awake() => AwakeCount++;
        public override void Start() => StartCount++;
        public override void Update(float deltaTime) => UpdateCount++;
        public override void LateUpdate(float deltaTime) => LateUpdateCount++;
        public override void OnGUI() => GuiCount++;
        public override void OnDestroy() => DestroyCount++;
    }

    public sealed class OtherComponent : Component
    {
    }

    public sealed class DestroyOnDestroyComponent : Component
    {
        public SceneManager Manager { get; set; } = null!;
        public SceneObject Target { get; set; } = null!;

        public override void OnDestroy() => Manager.DestroyObject(Target.Owner, Target);
    }
}
