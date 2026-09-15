using PathTracerApp.SceneGraph;

namespace PathTracerSceneGraph;

public class SceneManager(Scene scene)
{
    public SceneObject CreateObject()
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