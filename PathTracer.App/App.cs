using System.Numerics;
using Microsoft.Extensions.Logging;
using PathTracerApp.Renderer;
using PathTracerApp.SceneGraph;
using PathTracerSceneGraph;
using Silk.NET.Vulkan;

namespace PathTracerApp;

public class App(SceneLoop loop, SceneManager sceneManager)
{
    public void Run()
    {
        var scene = sceneManager.CreateScene();
        var obj = sceneManager.CreateObject(scene, "c1");
        var obj2 = sceneManager.CreateObject(scene, "c2");
        var obj3 = sceneManager.CreateObject(scene, "c3");
        AddCircle(obj, 100f, 0.5f, 0.45f);
        AddCircle(obj2, 100f, 0.47f, 0.55f);
        AddCircle(obj3, 100f, 0.53f, 0.55f);

        var mover = obj.AddComponent<TestMove>();
        mover.Speed = 0.05f;
        
        loop.Run(scene);
    }

    private void AddCircle(SceneObject obj, float r, float x, float y)
    {
        var trans = obj.AddComponent<Transform>();
        var circle = obj.AddComponent<CircleRenderer>();
        trans.Position = new Vector3(x, y, 0.0f);
        circle.Radius = r;
    }
}