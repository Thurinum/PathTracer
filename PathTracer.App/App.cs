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
        
        AddSphere(scene, new Vector3(-0.95f, -0.08f, -3.0f), 0.42f, new Color(0.78f, 0.12f, 0.08f), roughness: 0.75f);
        AddSphere(scene, new Vector3(0.0f, 0.12f, -2.65f), 0.62f, new Color(0.04f, 0.38f, 0.62f), roughness: 0.35f);
        AddSphere(scene, new Vector3(0.98f, -0.08f, -3.15f), 0.42f, new Color(0.86f, 0.53f, 0.12f), metallic: 0.82f, roughness: 0.04f);
        AddSphere(scene, new Vector3(-1.58f, -0.25f, -3.8f), 0.25f, new Color(0.12f, 0.62f, 0.42f), roughness: 0.65f);

        loop.Run(scene);
    }
    
    private void AddSphere(Scene scene, Vector3 position, float radius, Color color, float metallic = 0.0f, float roughness = 0.5f)
    {
        var obj = sceneManager.CreateObject(scene, "sphere");

        obj.Transform.Position = position;

        var sphere = obj.AddComponent<SphereComponent>();
        sphere.Radius = radius;
        sphere.Color = color;
        sphere.Metallic = metallic;
        sphere.Roughness = roughness;
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
