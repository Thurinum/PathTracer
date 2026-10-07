namespace Pathetic.Scene;

public interface IDestroyable
{
    public bool IsPendingDestroy { get; }
    public void Destroy();
}