using System.Numerics;
using PathTracerApp.Components;
using PathTracerApp.Renderer;
using PathTracerApp.SceneGraph;
using PathTracerSceneGraph;

namespace PathTracerApp;

public class App(SceneLoop loop, SceneManager sceneManager)
{
    public void Run()
    {
        var scene = sceneManager.CreateScene();

        sceneManager
            .CreateObject(scene, "fpsCounter")
            .AddComponent<FPSCounter>();

        for (int i = 0; i < 100; i++)
        {
            AddCircle(scene, Random.Shared.NextSingle() * (150 - 50), Random.Shared.NextSingle(), Random.Shared.NextSingle());
        }
        
        loop.Run(scene);
    }

    private void AddCircle(Scene scene, float r, float x, float y)
    {
        var obj = sceneManager.CreateObject(scene, "circle");

        var trans = obj.AddComponent<Transform>();
        trans.Position = new Vector3(x, y, 0.0f);
        
        var circle = obj.AddComponent<CircleRenderer>();
        circle.Radius = r;
        
        var mover = obj.AddComponent<TestMove>();
        mover.Speed = 0.2f;
    }
}