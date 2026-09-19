namespace PathTracerCore.SceneGraph;

public class SceneManager(ComponentFactory componentFactory)
{
    public Scene CreateScene()
    {
        return new Scene(componentFactory);
    }
    
    public SceneObject CreateObject(Scene scene, string name)
    {
        var obj = new SceneObject
        {
            Owner = scene,
            Name = name
        };
        scene.Objects.Add(obj);
        return obj;
    }

    public void DestroyObject(Scene scene, SceneObject obj)
    {
        if (obj.IsPendingDestroy)
            return;

        obj.IsPendingDestroy = true;
        scene.PendingDestroy.Add(obj);
    }
}