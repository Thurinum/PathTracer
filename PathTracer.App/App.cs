using System.Numerics;
using PathTracerApp.Components;
using PathTracerCore.Renderer;
using PathTracerCore.SceneGraph;

namespace PathTracerApp;

public class App(SceneLoop loop, SceneManager sceneManager)
{
    public void Run()
    {
        var scene = sceneManager.CreateScene();

        sceneManager
            .CreateObject(scene, "fpsCounter")
            .AddComponent<FPSCounter>();
        
        AddSphere(scene, new Vector3(0.0f, 0.0f, -1.0f), 0.5f, Color.Red);

        loop.Run(scene);
    }
    
    private void AddSphere(Scene scene, Vector3 position, float radius, Color color)
    {
        var obj = sceneManager.CreateObject(scene, "sphere");

        obj.Transform.Position = position;

        var sphere = obj.AddComponent<SphereComponent>();
        sphere.Radius = radius;
        sphere.Color = color;
    }
    
    private void AddCircle(Scene scene, float r, float x, float y)
    {
        var obj = sceneManager.CreateObject(scene, "circle");

        obj.Transform.Position = new Vector3(x, y, 0.0f);
        
        var circle = obj.AddComponent<CircleComponent>();
        circle.Radius = r;
        
        var mover = obj.AddComponent<TestMove>();
        mover.Speed = 0.2f;
    }
}