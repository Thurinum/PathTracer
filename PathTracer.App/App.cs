using PathTracerApp.Renderer;
using PathTracerApp.SceneGraph;
using PathTracerSceneGraph;

namespace PathTracerApp;

public class App(SceneLoop loop, SceneManager sceneManager)
{
    public void Run()
    {
        var scene = sceneManager.CreateScene();
        var circle = sceneManager.CreateObject(scene);
        circle.AddComponent<Transform>();
        circle.AddComponent<SphereRenderer>();
        
        loop.Run(scene);
    }    
}