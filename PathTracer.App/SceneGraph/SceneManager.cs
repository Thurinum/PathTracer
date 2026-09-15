using PathTracerSceneGraph;

namespace PathTracerApp.SceneGraph;

public class SceneManager(ComponentFactory componentFactory)
{
    public Scene CreateScene()
    {
        return new Scene(componentFactory);
    }
    
    public SceneObject CreateObject(Scene scene)
    {
        var obj = new SceneObject { Owner = scene };
        scene.Objects.Add(obj);
        return obj;
    }

    public void DestroyObject(SceneObject obj)
    {
        // TODO: Must wait until the end of the frame to delete
        throw new NotImplementedException();
    }
}