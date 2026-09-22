using System.Numerics;
using PathTracerApp.Components;
using PathTracerCore;
using PathTracerCore.Renderer.Camera;
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
            
        var camera = sceneManager.CreateObject(scene, "camera");
        camera.AddComponent<Transform>();
        camera.AddComponent<CameraComponent>();

        for (int i = 0; i < 100; i++)
        {
            AddCircle(scene, Random.Shared.NextSingle() * (150 - 50), Random.Shared.NextSingle(), Random.Shared.NextSingle());
        }
        
        var orbit = camera.AddComponent<OrbitCameraComponent>();
        orbit.Target = Vector3.Zero;
        orbit.Distance = 4f;
        orbit.ElevationDegrees = 20f;
        orbit.AzimuthDegrees = 0f;
        orbit.OrbitSpeedDegreesPerSecond = 0f;   // 0 = static; 30 = auto-orbit
        
        AddPlane(scene, new Vector3( 0, -1,  0), Quaternion.FromEuler(new Vector3(-90,   0,  0))); // floor  +Y
        AddPlane(scene, new Vector3( 0,  1,  0), Quaternion.FromEuler(new Vector3( 90,   0,  0))); // ceil   -Y
        AddPlane(scene, new Vector3(-1,  0,  0), Quaternion.FromEuler(new Vector3(  0,  90,  0))); // left   +X
        AddPlane(scene, new Vector3( 1,  0,  0), Quaternion.FromEuler(new Vector3(  0, -90,  0))); // right  -X
        AddPlane(scene, new Vector3( 0,  0, -1), Quaternion.FromEuler(new Vector3(  0,   0,  0))); // back   +Z
        
        loop.Run(scene);
    }

    private void AddPlane(Scene scene, Vector3 pos, Quaternion rot)
    {
        var obj = sceneManager.CreateObject(scene, "plane");
        var trans = obj.AddComponent<Transform>();
        trans.Position = pos;
        trans.Rotation = rot;
        
        var plane = obj.AddComponent<PlaneComponent>();
        plane.Width = 2f;
        plane.Height = 2f;

        // obj.AddComponent<RandomMover>();
    }

    private void AddCircle(Scene scene, float r, float x, float y)
    {
        var obj = sceneManager.CreateObject(scene, "circle");

        var trans = obj.AddComponent<Transform>();
        trans.Position = new Vector3(x, y, 0.0f);
        
        var circle = obj.AddComponent<CircleComponent>();
        circle.Radius = r;
        
        var mover = obj.AddComponent<RandomMover>();
        mover.Speed = 0.2f;
    }
}